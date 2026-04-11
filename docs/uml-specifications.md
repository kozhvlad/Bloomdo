# Спецификации UML-диаграмм — Bloomdo

> Детальная текстовая фактура для отрисовки 7 UML-диаграмм дипломного проекта.
> Каждая спецификация основана на реальном коде проекта и содержит точные имена классов, методов и полей.

---

## 1. Android Foreground Service — блокировка приложений в реальном времени

**Тип диаграммы:** State Machine Diagram

### Акторы / компоненты

| Компонент | Проект | Роль |
|-----------|--------|------|
| `AndroidBlockEnforcementService` | Client.Android | Запуск/остановка сервиса через `Context.StartForegroundService` |
| `BlockEnforcementForegroundService` | Client.Android | Foreground Service, основной цикл блокировки |
| `BlockRuleStore` | Client.Infrastructure | Хранение правил блокировки в `block_rules.json` |
| `GroupCompletionStore` | Client.Infrastructure | Хранение статуса выполнения групп в `group_completion.json` |
| `LocalUsageStore` | Client.Infrastructure | Сохранение usage-статистики в `usage_cache/` |
| `BlockedActivity` | Client.Android | Activity-экран "Приложение заблокировано" |
| Android `UsageStatsManager` | Android OS | Системный API доступа к usage-статистике |

### Состояния

1. **Stopped** — сервис не запущен.
2. **Initializing** — вызван `OnStartCommand`. Создаётся NotificationChannel (`bloomdo_enforcement`, importance = Low). Строится persistent Notification ("App blocking is active"). Вызывается `StartForeground(9001, notification, TypeSpecialUse)`. Загружаются правила из файла (`LoadRules`).
3. **Ticking** — основное состояние. `Timer` тикает каждые 2 секунды, вызывая `OnTick`.
4. **EvaluatingRules** — внутри `OnTick`. Загружаются правила (`LoadRules`) и статус групп (`LoadGroupCompletion`). Определяется foreground-пакет через `GetForegroundPackage`.
5. **RuleMatching** — для каждого правила из `_cachedRules` проверяется, подпадает ли текущий foreground-пакет под блокировку. Четыре ветки:
   - **Schedule**: проверка `IsInSchedule` — текущее время попадает в `[StartTime, EndTime]` с учётом дней недели и overnight-расписания (22:00–07:00).
   - **Limit**: проверка `IsOverDailyLimit` — запрос `UsageStatsManager.QueryAndAggregateUsageStats` от начала дня до текущего момента, сравнение `TotalTimeInForeground / 60000` с `DailyLimitMinutes`.
   - **Focus**: проверка `elapsed = UtcNow − FocusStartedAtUtc < FocusDurationMinutes`.
   - **Bloomdo**: проверка `_cachedGroupCompletion[RequiredActivityGroupId]` — если группа выполнена (`true`), приложение разблокировано; если `false` или ключ отсутствует — заблокировано.
6. **Blocking** — вызов `ShowBlockedScreen()` → запуск `BlockedActivity` с флагами `NewTask | ClearTop`. Запоминается `_lastBlockedPackage`, чтобы не перезапускать Activity повторно для того же пакета.
7. **Passing** — пакет не подпадает под блокировку. `_lastBlockedPackage = null`.
8. **SavingUsage** — если прошло ≥ 15 минут с последнего сохранения (`UsageSaveInterval`), вызывается `SaveUsageLocallyIfDue()`: запрос `QueryAndAggregateUsageStats`, фильтрация по `GetLaunchIntentForPackage != null` и `TotalTimeInForeground > 0`, сохранение через `LocalUsageStore.SaveSnapshotDirect(today, 0, apps)`.
9. **Destroyed** — вызов `OnDestroy`, таймер уничтожается (`_timer?.Dispose()`).

### Переходы

| Из | В | Триггер / условие |
|----|---|-------------------|
| Stopped | Initializing | `AndroidBlockEnforcementService.Start()` → `Context.StartForegroundService(intent)` |
| Initializing | Ticking | Создан `Timer(OnTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(2))` |
| Ticking | EvaluatingRules | Каждые 2 секунды (тик таймера) |
| EvaluatingRules | Ticking | `_cachedRules.Count == 0` (нет правил — пропуск) |
| EvaluatingRules | Ticking | `foregroundPkg` = null или пустая строка |
| EvaluatingRules | Ticking | `foregroundPkg == "com.CompanyName.Bloomdo.Client"` (никогда не блокируем собственное приложение) |
| EvaluatingRules | RuleMatching | Есть foreground-пакет и есть правила |
| RuleMatching | Blocking | `ShouldBlock(foregroundPkg) == true` И `_lastBlockedPackage != foregroundPkg` |
| RuleMatching | Ticking | `ShouldBlock(foregroundPkg) == true` И `_lastBlockedPackage == foregroundPkg` (экран уже показан) |
| RuleMatching | Passing | `ShouldBlock(foregroundPkg) == false` |
| Blocking | SavingUsage | После показа экрана блокировки |
| Passing | SavingUsage | После определения "не блокировать" |
| SavingUsage | Ticking | `DateTime.UtcNow − _lastUsageSaveUtc < 15 мин` (пропуск сохранения) |
| SavingUsage | Ticking | Сохранение выполнено или `DateTime.UtcNow − _lastUsageSaveUtc ≥ 15 мин` → сохранение + возврат |
| Ticking | Destroyed | `AndroidBlockEnforcementService.Stop()` → `Context.StopService(intent)` |
| Любое | Ticking | Исключение в `OnTick` — перехватывается в `catch`, логируется через `Log.Warn` |

### Ключевые детали для отрисовки

- Внутри состояния **RuleMatching** показать 4 ветки (Schedule / Limit / Focus / Bloomdo) как internal transitions или nested states.
- **IPC через файловую систему**: `BlockRuleStore` и `GroupCompletionStore` записывают JSON-файлы из основного процесса приложения (UI), а `BlockEnforcementForegroundService` читает их напрямую (`System.IO.File.ReadAllText`) — сервис живёт вне DI-контейнера.
- Guard condition на `foregroundPkg == OwnPackage` — самозащита.
- Overnight schedule: `start > end → currentTime >= start || currentTime <= end`.

---

## 2. JWT Token Lifecycle — proactive refresh + retry chain

**Тип диаграммы:** Sequence Diagram

### Акторы / участники (lifelines)

| Lifeline | Класс | Проект |
|----------|-------|--------|
| Client UI | Любая ViewModel (напр. `HomeViewModel`) | Client.Application |
| AuthHeaderHandler | `AuthHeaderHandler : DelegatingHandler` | Client.Infrastructure |
| AccessTokenManager | `AccessTokenManager : IAccessTokenManager` | Client.Infrastructure |
| HttpClient | `System.Net.Http.HttpClient` | .NET |
| API Controller | Любой контроллер (напр. `DailyActivitiesController`) | Server.Api |
| AuthService | `AuthService : IAuthService` | Server.Application |
| DB (RefreshToken) | `IRepository<RefreshToken>` | Server.Infrastructure |

### Сценарий A: Proactive Refresh (основной happy path)

1. **Client UI** → **AuthHeaderHandler**: `SendAsync(request)` (любой API-вызов).
2. **AuthHeaderHandler** проверяет: `AccessTokenManager.IsAccessTokenExpiringSoon` (= `_accessTokenExpiresAt <= UtcNow + 30 сек`) И `IsAuthenticated == true`.
3. [alt: токен истекает скоро]
   - **AuthHeaderHandler** → `RefreshLock.WaitAsync()` (SemaphoreSlim(1,1)).
   - **AuthHeaderHandler** проверяет double-check: `IsAccessTokenExpiringSoon` (мог обновиться другим потоком).
   - [alt: всё ещё истекает]
     - **AuthHeaderHandler** → **AccessTokenManager**: `RefreshTokenAsync()`.
     - **AccessTokenManager** → **AuthApiService**: `RefreshTokenAsync(_refreshToken)` — HTTP POST.
     - **AuthApiService** → **API (AuthController)**: `POST /api/auth/refresh`.
     - **AuthController** → **AuthService**: `RefreshTokenAsync(refreshToken, ipAddress)`.
     - **AuthService** → **DB**: `FirstOrDefaultAsync(rt => rt.Token == refreshToken)`.
     - [alt: токен найден и активен]
       - **AuthService**: генерирует новый refresh token (`_jwtService.GenerateRefreshToken()`).
       - **AuthService** → **DB**: `AddAsync(newToken)` — сначала сохраняется новый (user не теряет доступ).
       - **AuthService** → **DB**: `UpdateAsync(oldToken)` — revoke: `IsRevoked = true`, `RevokedAt = UtcNow`, `ReplacedByToken = newRefreshToken`.
       - **AuthService**: генерирует новый access token (`_jwtService.GenerateAccessToken(accountId, email, roles, permissions)`).
       - **AuthService** → **AuthController**: возвращает `AuthResponse`.
     - **AccessTokenManager**: `ApplyAuthResponse(response)` — обновляет `_accessToken`, `_refreshToken`, `_accessTokenExpiresAt`, `_currentRoles`, `_currentPermissions`.
     - **AccessTokenManager** → **TokenStorage**: `SaveTokensAsync(accessToken, refreshToken)`.
   - **AuthHeaderHandler**: `RefreshLock.Release()`.
   - `didProactiveRefresh = true`.
4. **AuthHeaderHandler**: `AttachToken(request)` — `request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenManager.AuthToken)`.
5. **AuthHeaderHandler** → **HttpClient** → **API Controller**: `base.SendAsync(request)`.
6. **API Controller** возвращает `200 OK` с данными.
7. **AuthHeaderHandler** → **Client UI**: возвращает response.

### Сценарий B: Reactive Refresh (401 без proactive)

1. Шаги 1-5, но `IsAccessTokenExpiringSoon == false`, поэтому proactive refresh пропускается. `didProactiveRefresh = false`.
2. **API Controller** возвращает `401 Unauthorized`.
3. **AuthHeaderHandler** проверяет: `response.StatusCode == 401` И `IsAuthenticated == true` И `didProactiveRefresh == false`.
4. **AuthHeaderHandler** → **AccessTokenManager**: `RefreshTokenAsync()` (аналогично сценарию A).
5. [alt: refresh успешен]
   - **AuthHeaderHandler**: клонирует запрос (`CloneRequestAsync`), прикрепляет новый токен.
   - **AuthHeaderHandler** → **HttpClient**: повторяет `base.SendAsync(retryRequest)`.
   - Возвращает результат повторного запроса.
6. [alt: refresh неуспешен]
   - Возвращает оригинальный `401`.

### Сценарий C: Clock-Skew Retry (401 после proactive refresh)

1. Proactive refresh выполнен (`didProactiveRefresh = true`), но API всё равно вернул `401`.
2. **AuthHeaderHandler**: клонирует запрос, прикрепляет тот же свежий токен.
3. **AuthHeaderHandler**: `await Task.Delay(500)` — задержка для компенсации clock skew.
4. **AuthHeaderHandler** → **HttpClient**: повторяет запрос.
5. [alt: всё ещё 401]
   - **AuthHeaderHandler** → **AccessTokenManager**: `RefreshTokenAsync()` — полный refresh как последняя попытка.
   - [alt: refresh успешен] — клонирует и повторяет запрос.
   - [alt: refresh неуспешен] — возвращает `401`.
6. [alt: успех] — возвращает ответ.

### Сценарий D: Grace Period для дублирующих запросов (серверная сторона)

1. Два параллельных HTTP-запроса клиента оба отправляют `RefreshTokenAsync` с одним и тем же refresh token.
2. Первый запрос: успешно ротирует токен (oldToken revoked, newToken создан).
3. Второй запрос: `AuthService.RefreshTokenAsync` находит токен, но `IsActive == false` и `IsRevoked == true`.
4. **AuthService** проверяет grace period: `token.ReplacedByToken != null` И `(UtcNow − token.RevokedAt) < 30 сек`.
5. [alt: grace period активен]
   - **AuthService** → **DB**: `FirstOrDefaultAsync(rt => rt.Token == token.ReplacedByToken)`.
   - [alt: replacement token активен] — генерирует новый access token для того же аккаунта, возвращает `AuthResponse` с replacement refresh token.
6. [alt: grace period истёк] — возвращает `null` (сессия инвалидирована).

### Сценарий E: Session Invalidation

1. `RefreshTokenAsync` возвращает `null` (токен отвергнут сервером).
2. **AccessTokenManager**: `ClearState()` — обнуляет `_accessToken`, `_refreshToken`, `_currentUser`, `_currentRoles`, `_currentPermissions`.
3. **AccessTokenManager** → **TokenStorage**: `ClearTokensAsync()`.
4. **AccessTokenManager**: вызывает `SessionInvalidated?.Invoke()` — событие.
5. **ShellViewModel** (подписчик): перенаправляет пользователя на `LoginViewModel`.

### Ключевые детали для отрисовки

- `SemaphoreSlim RefreshLock` — показать как combined fragment `critical` или аннотацию.
- Double-check pattern внутри `TryRefreshTokenAsync`.
- На сервере: порядок операций — **сначала `AddAsync(newToken)`, потом `UpdateAsync(oldToken)` с revoke** — обеспечивает отсутствие окна, когда у пользователя нет валидного токена.
- Grace period (30 сек) — показать как `opt` фрагмент.
- `CloneRequestAsync` — метод полностью копирует `HttpRequestMessage` (headers, content, options, version), т.к. `HttpRequestMessage` нельзя отправить дважды.

---

## 3. AI Photo Verification — end-to-end pipeline

**Тип диаграммы:** Sequence Diagram

### Акторы / участники (lifelines)

| Lifeline | Класс | Проект |
|----------|-------|--------|
| User | Физический пользователь | — |
| HomeViewModel | `HomeViewModel` | Client.Application |
| PhotoVerificationDialogService | `PhotoVerificationDialogService` | Client.UI |
| PhotoVerificationViewModel | `PhotoVerificationViewModel` | Client.Application |
| IImagePickerService | Платформенная реализация (камера/галерея) | Client.UI |
| DailyActivityApiService | `DailyActivityApiService` | Client.Infrastructure |
| DailyActivitiesController | `DailyActivitiesController` | Server.Api |
| DailyActivityService | `DailyActivityService` | Server.Application |
| GeminiVisionService | `GeminiVisionService : IVisionService` | Server.Application |
| Gemini API | Google Gemini 2.5 Flash | Внешний сервис |

### Основной сценарий (Happy Path)

1. **User** нажимает кнопку верификации на задаче типа `PhotoVerification`.
2. **HomeViewModel** → `OpenPhotoVerification(task)`:
   - Проверяет: `task.IsCompleted` → если да, показывает toast "Already verified today" и выходит.
   - Вызывает `_photoVerificationDialogService.Show(task.Id, task.VerificationTemplate, task.CustomVerificationCriteria, date, onVerified: () => LoadDailyActivitiesAsync())`.
3. **PhotoVerificationDialogService**:
   - Создаёт `new PhotoVerificationViewModel(activityApi, imagePicker, toastService, onVerified: () => { onVerified(); CloseOverlay(); })`.
   - Вызывает `vm.Configure(activityItemId, date)` — сброс состояния: `State = Idle`, `Explanation = ""`, `PreviewImageBytes = null`, `_imageBytes = null`.
   - Устанавливает `shell.OnOverlayClosed = CloseOverlay` и `shell.OverlayContent = vm` — overlay появляется в UI.
4. **User** выбирает источник фото.
5. [alt: "Pick from Gallery"]
   - **PhotoVerificationViewModel** → **IImagePickerService**: `PickFromGalleryAsync(ct)`.
   - Возвращает `ImagePickResult { CompressedBytes }`.
6. [alt: "Take Photo"]
   - **PhotoVerificationViewModel** → **IImagePickerService**: `TakePhotoAsync(ct)`.
   - Возвращает `ImagePickResult { CompressedBytes }`.
7. **PhotoVerificationViewModel**: `_imageBytes = result.CompressedBytes`, `PreviewImageBytes = result.CompressedBytes`, `State = HasPhoto`.
8. **User** нажимает "Verify".
9. **PhotoVerificationViewModel** → `VerifyCommand`:
   - `State = Verifying`.
   - Создаёт `VerifyPhotoRequest { ActivityItemId, Date, ImageBase64 = Convert.ToBase64String(_imageBytes) }`.
   - → **DailyActivityApiService**: `VerifyPhotoAsync(request, ct)`.
10. **DailyActivityApiService** → **DailyActivitiesController**: `POST /api/activities/verify-photo` с JSON-телом.
11. **DailyActivitiesController** → **DailyActivityService**: `VerifyPhotoAsync(accountId, request, ct)`.
12. **DailyActivityService**:
    - Загружает `ActivityItem` по `request.ActivityItemId`.
    - Проверяет ownership: `isOwner` (через `groupRepo`) или `isMember` (через `membershipRepo`, `Status == Accepted`). Если ни то ни другое — `InvalidOperationException`.
    - Декодирует изображение: `Convert.FromBase64String(request.ImageBase64)`.
    - Определяет template: если `item.VerificationTemplateId` задан — приводит к `VerificationTemplate`, иначе `Custom`.
    - → **GeminiVisionService**: `VerifyAsync(imageBytes, template, item.CustomVerificationCriteria, ct)`.
13. **GeminiVisionService**:
    - Определяет критерии по шаблону из словаря `TemplateCriteria` (12 шаблонов: `Workout`, `Meal`, `Workspace`, `Reading`, `Outdoors`, `Sleep`, `Meditation`, `Cleaning`, `Water`, `ColdShower`, `Study`, `Custom`). Для `Custom` — использует `customCriteria`.
    - Формирует prompt: `"You are a photo verification assistant. Analyze the provided image and determine if it shows: {criteria}. Respond ONLY with valid JSON..."`.
    - Создаёт `Contents` с двумя Part: `Text` (prompt) и `InlineData` (Blob с `MimeType = "image/jpeg"`, `Data = imageBytes`).
    - `GenerateContentConfig { Temperature = 0.1f, MaxOutputTokens = 256 }`.
    - **Цикл по API-ключам** (`geminiSettings.ApiKeys`):
      - `var client = new Client(apiKey: apiKeys[i])`.
      - → **Gemini API**: `client.Models.GenerateContentAsync(model: "gemini-2.5-flash", contents, config)`.
      - [alt: успех] — извлекает `response.Candidates[0].Content.Parts[0].Text`. Выходит из цикла.
      - [alt: `ClientError` и `i < apiKeys.Count - 1`] — ключ rate-limited, переходит к следующему.
    - Если все ключи исчерпаны — `throw InvalidOperationException("all API keys exhausted")`.
    - `ParseResult(text)`:
      - Удаляет markdown code blocks (` ```json ... ``` `).
      - Десериализует в `GeminiVisionResponse { Verified: bool, Confidence: float, Explanation: string }`.
      - Логика принятия решения:
        - `Verified == true` И `Confidence >= 0.65` → `VerificationStatus.Verified`.
        - `Verified == true` И `Confidence < 0.65` → `VerificationStatus.LowConfidence`.
        - `Verified == false` → `VerificationStatus.Rejected`.
      - Возвращает `VisionResult(status, explanation, confidence)`.
14. **DailyActivityService** получает `VisionResult`:
    - [alt: `status == Verified`]
      - Автоматически вызывает `ToggleCompletionAsync(accountId, new ToggleCompletionRequest { ActivityItemId, Date })` — создаёт `ActivityCompletion` в БД.
      - Затем вызывает `statsService.RecalculateGoalMetAsync(accountId, date)` — пересчитывает, выполнена ли дневная цель.
    - Возвращает `VerifyPhotoResponse { Status, Explanation }`.
15. Ответ передаётся обратно по цепочке до **PhotoVerificationViewModel**.
16. **PhotoVerificationViewModel**:
    - `Explanation = response.Explanation`.
    - State переключается:
      - `Verified` → `PhotoVerificationState.Verified`. Вызывается `_onVerified()`.
      - `LowConfidence` → `PhotoVerificationState.LowConfidence`.
      - `Rejected` → `PhotoVerificationState.Rejected`.
    - Если `Verified` — callback `_onVerified` → `onVerified?.Invoke()` (в `HomeViewModel` перезагружает daily) + `CloseOverlay()` → `shell.OverlayContent = null`.

### Альтернативный сценарий: Retry

1. Из состояния `Rejected` или `LowConfidence` или `Error`, **User** нажимает "Retry".
2. `RetryCommand`: `State = Idle`, `Explanation = ""`, `PreviewImageBytes = null`, `_imageBytes = null`.
3. Пользователь начинает с шага 4.

### Альтернативный сценарий: Ошибка сети / API

1. `VerifyPhotoAsync` выбрасывает исключение.
2. `State = Error`, `Explanation = ""`.

### Ключевые детали для отрисовки

- Показать **state machine** `PhotoVerificationViewModel` как note: `Idle → HasPhoto → Verifying → Verified / LowConfidence / Rejected / Error`.
- **Multi-key fallback** в `GeminiVisionService` — показать loop fragment.
- **Confidence threshold 0.65** — показать как decision/alt.
- **Автоматическое создание `ActivityCompletion`** при `Verified` — важный side-effect.
- `CloseOverlay` — callback chain: ViewModel → DialogService → ShellViewModel.

---

## 4. Stripe Subscription Lifecycle — checkout + webhooks

**Тип диаграммы:** Sequence Diagram + State Machine Diagram (пара)

### 4A. Sequence Diagram — Checkout Flow

#### Акторы / участники (lifelines)

| Lifeline | Класс | Проект |
|----------|-------|--------|
| User | Физический пользователь | — |
| SubscriptionViewModel | `SubscriptionViewModel` | Client.Application |
| SubscriptionApiService | `SubscriptionApiService` | Client.Infrastructure |
| SubscriptionController | `SubscriptionController` | Server.Api |
| SubscriptionService | `SubscriptionService` | Server.Application |
| Stripe API | Stripe SDK (`CustomerService`, `SessionService`, `SubscriptionService`) | Внешний сервис |
| Browser | Системный браузер (Android) | — |

#### Сценарий: Создание подписки (Happy Path)

1. **User** открывает экран подписки.
2. **SubscriptionViewModel** → `OnAppearing()`:
   - Проверяет `_connectivityService.IsOnline`.
   - [alt: online] → `LoadStatusAsync()` → `_subscriptionApiService.GetStatusAsync()` → `GET /api/subscription/status`.
   - [alt: offline] → `LoadStatusFromCacheAsync()` → `_localSubscriptionStore.LoadAsync()`.
3. **SubscriptionController** → **SubscriptionService**: `GetStatusAsync(accountId)`.
4. **SubscriptionService**:
   - Загружает `Subscription` по `accountId`.
   - Проверяет: если `Status == Active` и `CurrentPeriodEnd < UtcNow` → `Status = Expired`, `UpdateAsync`.
   - Вызывает `BuildLimitsAsync(accountId, isPremium)`:
     - [alt: isPremium] — `MaxDailyChatMessages = int.MaxValue`, `MaxBlockRules = int.MaxValue`, `CanCustomizeEmoji = true`, `CanCustomizeColors = true`, `CanViewWeeklyStats = true`, подсчёт `RemainingStreakFreezes = max(0, MonthlyStreakFreezes − CountMonthlyFreezesUsed)`.
     - [alt: free] — `MaxDailyChatMessages = freeLimitsSettings.MaxDailyChatMessages`, подсчёт `RemainingChatMessagesToday = max(0, limit − todayMessages)`, `CurrentBlockRuleCount` из БД.
   - Возвращает `SubscriptionStatusResponse { Status, Plan, CurrentPeriodEnd, IsPremium, WillCancel, Limits }`.
5. **SubscriptionViewModel**: `ApplyStatus(status)` — обновляет UI. Кэширует через `_localSubscriptionStore.SaveAsync(status)`.
6. **User** выбирает план (Monthly / Yearly) и нажимает "Subscribe".
7. **SubscriptionViewModel** → `CheckoutCommand`:
   - `IsCheckoutLoading = true`.
   - → `_subscriptionApiService.CreateCheckoutSessionAsync(plan)` → `POST /api/subscription/checkout` с `CreateCheckoutSessionRequest { Plan }`.
8. **SubscriptionController** → **SubscriptionService**: `CreateCheckoutSessionAsync(accountId, email, plan, serverBaseUrl)`.
9. **SubscriptionService**:
   - `var stripeClient = new StripeClient(stripeSettings.SecretKey)`.
   - Загружает существующую подписку.
   - [alt: `subscription?.StripeCustomerId != null`] — использует существующий `customerId`.
   - [alt: нет Stripe Customer]
     - → **Stripe API**: `new CustomerService(stripeClient).CreateAsync(new CustomerCreateOptions { Email, Metadata = { accountId } })`.
     - `customerId = customer.Id`.
   - Определяет `priceId`: `Monthly → stripeSettings.MonthlyPriceId`, `Yearly → stripeSettings.YearlyPriceId`.
   - → **Stripe API**: `new SessionService(stripeClient).CreateAsync(new SessionCreateOptions { Customer, Mode = "subscription", PaymentMethodTypes = ["card"], LineItems = [{ Price = priceId, Quantity = 1 }], SuccessUrl = "{serverBaseUrl}/api/subscription/checkout-success?session_id={CHECKOUT_SESSION_ID}", CancelUrl = "{serverBaseUrl}/api/subscription/checkout-cancel", Metadata = { accountId, plan } })`.
   - Возвращает `CreateCheckoutSessionResponse { CheckoutUrl, SessionId }`.
10. **SubscriptionViewModel**: → **Browser**: `_browserService.OpenAsync(response.CheckoutUrl)` — открывает Stripe Checkout в системном браузере.
11. **User** заполняет данные карты в Stripe Checkout и оплачивает.
12. Stripe перенаправляет на `SuccessUrl` → **SubscriptionController** `GET /api/subscription/checkout-success` → возвращает HTML-страницу "Payment Successful! You can close this tab and return to the app."

#### Сценарий: Webhook Processing

13. **Stripe API** → **SubscriptionController**: `POST /api/subscription/webhook` (AllowAnonymous).
    - Тело: raw JSON event. Заголовок: `Stripe-Signature`.
14. **SubscriptionController**: читает body через `StreamReader`, передаёт `json` и `signature` в **SubscriptionService**: `HandleWebhookAsync(json, stripeSignature)`.
15. **SubscriptionService**:
    - [alt: `webhookSecret` не пустой] → `EventUtility.ConstructEvent(json, stripeSignature, webhookSecret)` — верификация подписи.
    - [alt: `webhookSecret` пустой или `StripeException`] → `EventUtility.ParseEvent(json)` — без верификации (dev-режим).
    - Switch по `stripeEvent.Type`:
      - **`checkout.session.completed`** → `HandleCheckoutSessionCompleted`:
        - Извлекает `session.Metadata["accountId"]` и `session.Metadata["plan"]`.
        - Валидирует `Guid.TryParse` и `Enum.TryParse<SubscriptionPlan>`.
        - [alt: подписка существует] → обновляет: `StripeCustomerId`, `StripeSubscriptionId`, `Plan`, `Status = Active`, `CancelAtPeriodEnd = false`, `CurrentPeriodStart = UtcNow`, `CurrentPeriodEnd = Monthly ? +1 мес : +1 год`.
        - [alt: новая] → создаёт `new Subscription { ... }`, `CreateAsync`.
      - **`customer.subscription.updated`** → `HandleSubscriptionUpdated`:
        - Находит подписку по `stripeSub.Id` (`GetByStripeSubscriptionIdAsync`).
        - Маппинг статуса: `"active" → Active`, `"past_due" → PastDue`, `"canceled" → Cancelled`.
        - Обновляет `CancelAtPeriodEnd`.
      - **`customer.subscription.deleted`** → `HandleSubscriptionDeleted`:
        - `Status = Expired`, `CancelAtPeriodEnd = false`.
      - **`invoice.payment_failed`** → `HandlePaymentFailed`:
        - Извлекает `stripeSubId` из `invoice.Parent.SubscriptionDetails.SubscriptionId`.
        - `Status = PastDue`.

#### Сценарий: Отмена подписки

16. **User** нажимает "Cancel Subscription".
17. **SubscriptionViewModel** → `_subscriptionApiService.CancelSubscriptionAsync()` → `POST /api/subscription/cancel`.
18. **SubscriptionService**: `CancelSubscriptionAsync(accountId)`:
    - Загружает подписку, проверяет `StripeSubscriptionId != null`.
    - → **Stripe API**: `new SubscriptionService(stripeClient).UpdateAsync(subscriptionId, new SubscriptionUpdateOptions { CancelAtPeriodEnd = true })`.
    - Обновляет локально: `CancelAtPeriodEnd = true`, `CancelledAt = UtcNow`.

### 4B. State Machine Diagram — Жизненный цикл подписки

#### Состояния

| Состояние | Поле `SubscriptionStatus` | Описание |
|-----------|--------------------------|----------|
| **None** | `SubscriptionStatus.None` | Подписки нет (свежий аккаунт). `subscription == null`. |
| **Active** | `SubscriptionStatus.Active` | Подписка активна. `CurrentPeriodEnd > UtcNow`. |
| **Active (CancelPending)** | `Active` + `CancelAtPeriodEnd = true` | Подписка активна, но будет отменена в конце периода. |
| **PastDue** | `SubscriptionStatus.PastDue` | Оплата не прошла. Stripe пытается повторить. |
| **Cancelled** | `SubscriptionStatus.Cancelled` | Подписка отменена Stripe. |
| **Expired** | `SubscriptionStatus.Expired` | Период истёк, либо подписка удалена в Stripe. |

#### Переходы

| Из | В | Триггер |
|----|---|---------|
| None | Active | Webhook `checkout.session.completed` |
| Active | Active (CancelPending) | API `CancelSubscriptionAsync` → Stripe `UpdateAsync(CancelAtPeriodEnd = true)` |
| Active (CancelPending) | Active | Webhook `customer.subscription.updated` с `CancelAtPeriodEnd = false` (пользователь передумал) |
| Active | PastDue | Webhook `invoice.payment_failed` |
| Active | Expired | `GetStatusAsync` проверяет `CurrentPeriodEnd < UtcNow` → `Status = Expired` |
| Active (CancelPending) | Expired | Webhook `customer.subscription.deleted` (период закончился) |
| PastDue | Active | Webhook `customer.subscription.updated` со `status = "active"` (платёж прошёл) |
| PastDue | Cancelled | Webhook `customer.subscription.updated` со `status = "canceled"` (Stripe сдался) |
| Cancelled | Expired | Webhook `customer.subscription.deleted` |
| Expired | Active | Webhook `checkout.session.completed` (повторная подписка) |

### Ключевые детали для отрисовки

- **Три системы**: Client ↔ Server ↔ Stripe — показать три зоны или три столбца.
- **Webhook — асинхронный**: Stripe отправляет webhook через отдельный HTTP-вызов, не связанный с сессией пользователя.
- **Влияние на бизнес-логику**: `BuildLimitsAsync` — показать как note: лимиты чата, блок-правил, кастомизации, streak freezes.
- **Offline fallback**: `SubscriptionViewModel` кэширует статус в `LocalSubscriptionStore` и восстанавливает при offline.

---

## 5. Offline-First Data Sync — Usage + Activity Cache

**Тип диаграммы:** Activity Diagram

### Акторы / компоненты

| Компонент | Проект | Роль |
|-----------|--------|------|
| `BlockEnforcementForegroundService` | Client.Android | Периодическое сохранение usage каждые 15 мин |
| `AndroidAppUsageService` | Client.Android | Получение usage через `UsageStatsManager` |
| `UsageSyncService` | Client.Application | Оркестрирует сохранение и синхронизацию |
| `LocalUsageStore` | Client.Infrastructure | Файловый кэш usage (`usage_cache/usage_YYYY-MM-DD.json`) |
| `LocalActivityCache` | Client.Infrastructure | Файловый кэш activities (`activity_cache/daily_YYYY-MM-DD.json`) + очередь `pending_toggles.json` |
| `HomeViewModel` | Client.Application | Загрузка daily activities с offline fallback |
| `StatsApiService` | Client.Infrastructure | HTTP-клиент для `POST /api/stats/sync-usage` |
| `ConnectivityService` | Client.Infrastructure | Проверка `IsOnline` |

### Поток 1: Сохранение usage из Foreground Service (фоновый, без DI)

1. **Start**: тик таймера в `BlockEnforcementForegroundService` (каждые 2 сек).
2. **Decision**: `DateTime.UtcNow − _lastUsageSaveUtc >= 15 мин`?
   - [Нет] → **End** (пропуск).
   - [Да] → продолжение.
3. `_lastUsageSaveUtc = DateTime.UtcNow`.
4. **Action**: `UsageStatsManager.QueryAndAggregateUsageStats(startOfDay, now)`.
5. **Loop**: для каждого `(pkg, usageStats)` в результате:
   - **Decision**: `PackageManager.GetLaunchIntentForPackage(pkg) == null`?
     - [Да] → пропуск (системный сервис).
   - **Decision**: `usageStats.TotalTimeInForeground <= 0`?
     - [Да] → пропуск.
   - **Action**: получение `AppLabel` через `PackageManager.GetApplicationLabel`.
   - Добавление `LocalAppUsageEntry { PackageName, AppLabel, ForegroundSeconds = fgMs / 1000 }` в список.
6. **Decision**: `apps.Count > 0`?
   - [Нет] → **End**.
   - [Да] → `LocalUsageStore.SaveSnapshotDirect(today, 0, apps)`.
7. **Action** `SaveSnapshotDirect` (static, без DI):
   - `EnsureCacheDir()` → создаёт `usage_cache/` если не существует.
   - Чтение существующего файла (если есть) для сохранения флага `SyncedToServer`.
   - Создание `LocalUsageSnapshot { Date, LastUpdatedUtc, Pickups = 0, SyncedToServer = false, Apps }`.
   - `File.WriteAllText(filePath, json)` — **синхронная** запись (foreground service thread).
8. **End**.

### Поток 2: Синхронизация usage на сервер (UI thread, async)

1. **Start**: вызов `UsageSyncService.SyncToServerAsync()` (при появлении HomeViewModel или по таймеру).
2. **Action**: `SaveLocalSnapshotAsync()`:
   - [alt: `appUsageService != null`] →  `appUsageService.GetTodayUsageAsync()` + `GetPickupsTodayAsync()`.
   - Создание `LocalUsageSnapshot { Date = today, Apps = usage.Select(...), Pickups, SyncedToServer = false }`.
   - → `localUsageStore.SaveSnapshotAsync(snapshot)` — async запись с `SemaphoreSlim Lock`.
3. **Action**: `localUsageStore.LoadSnapshotAsync(today)`.
4. **Decision**: `snapshot == null || snapshot.Apps.Count == 0`?
   - [Да] → **End**.
   - [Нет] → продолжение.
5. **Action**: `BuildSyncRequest(snapshot)` → `SyncUsageRequest { Date, Pickups, Apps = [...] }`.
6. **Action**: → `statsApiService.SyncUsageAsync(request)` → `POST /api/stats/sync-usage`.
7. **Decision**: `success == true`?
   - [Да] → `localUsageStore.MarkSyncedAsync(today)` — загружает snapshot, ставит `SyncedToServer = true`, перезаписывает.
   - [Нет] → **End** (попробует позже).
8. **End**.

### Поток 3: Синхронизация пропущенных дней (pending sync)

1. **Start**: вызов `UsageSyncService.SyncPendingAsync()`.
2. **Action**: `SaveLocalSnapshotAsync()` — сохранить текущие данные (на случай, если ещё не сохранены).
3. **Action**: `localUsageStore.GetUnsyncedSnapshotsAsync()`:
   - Сканирование всех файлов `usage_cache/usage_*.json`.
   - Десериализация каждого, фильтрация по `SyncedToServer == false`.
4. **Decision**: `unsynced.Count == 0`?
   - [Да] → **End**.
   - [Нет] → продолжение.
5. **Loop**: для каждого `snapshot` в `unsynced`:
   - **Decision**: `snapshot.Apps.Count == 0`?
     - [Да] → `continue`.
   - **Action**: `BuildSyncRequest(snapshot)` → `statsApiService.SyncUsageAsync(request)`.
   - **Decision**: `success`?
     - [Да] → `localUsageStore.MarkSyncedAsync(snapshot.Date)`.
     - [Нет] → `catch`, логирование, `continue` (не прерывать цикл из-за одного дня).
6. **End**.

### Поток 4: Offline-first загрузка Activity (HomeViewModel)

1. **Start**: `HomeViewModel.LoadDailyActivitiesAsync()`.
2. `IsOffline = !_connectivityService.IsOnline`.
3. **Decision**: `IsOffline == false`?
   - [Да: Online] → Try:
     - `daily = await _activityApi.GetDailyAsync()`.
     - [alt: успех] → `_localActivityCache.SaveDailyAsync(daily, today)` (кэширование для offline).
     - [alt: `HttpRequestException` / `TaskCanceledException`] → `IsOffline = true`, fallback.
   - [Нет: Offline] → fallback.
4. **Decision** (fallback): `daily == null` и `_localActivityCache != null`?
   - [Да] → `daily = await _localActivityCache.LoadDailyAsync(today)`.
5. Построение UI из `daily`.
6. **End**.

### Поток 5: Offline toggle → pending queue → replay

1. **Start**: `HomeViewModel.ToggleTask(task)` при `IsOffline == true` (или `HttpRequestException`).
2. **Action**: `ApplyToggleOfflineAsync(task, request)`:
   - Оптимистичное обновление UI: `task.IsCompleted = !task.IsCompleted`.
   - `_localActivityCache.EnqueueToggleAsync(new PendingActivityToggle { ActivityItemId, Date, CountValue, CreatedAtUtc })`:
     - Внутри: `SemaphoreSlim Lock`, чтение `pending_toggles.json`, добавление в список, перезапись файла.
   - Toast: "Saved offline — will sync when connected".
3. При восстановлении соединения (следующий `OnAppearing` или connectivity event):
   - `_localActivityCache.LoadPendingTogglesAsync()` → replay каждого toggle через API.
   - `_localActivityCache.ClearPendingTogglesAsync()`.
4. **End**.

### Ключевые детали для отрисовки

- **Два параллельных потока записи**: Foreground Service (sync `File.WriteAllText`, без DI) и UI (async `File.WriteAllTextAsync`, через `SemaphoreSlim`). Показать fork/join.
- **SemaphoreSlim** — показать как synchronization bar или аннотацию.
- `SaveSnapshotDirect` — **static** метод, т.к. Foreground Service не имеет доступа к DI. Показать как отдельный swimlane.
- `SyncedToServer` flag — важный маркер для избежания дублирования.
- `CleanupOldFiles(keepDays: 14)` — самоочистка кэша.
- Decision node: "online?" в нескольких точках (перед API-вызовом, при ошибке сети).

---

## 6. SignalR Real-Time Social — группы + уведомления

**Тип диаграммы:** Sequence Diagram

### Акторы / участники (lifelines)

| Lifeline | Роль |
|----------|------|
| User A (Client) | Инициатор действия (напр. выполняет задачу) |
| User A — SignalRClientService | `SignalRClientService` на устройстве User A |
| Server — SocialHub | `SocialHub : Hub` (SignalR Hub, `[Authorize]`) |
| Server — SocialRealTimeNotifier | `SocialRealTimeNotifier` (использует `IHubContext<SocialHub>`) |
| Server — Business Logic | Сервисы (`SocialService`, `DailyActivityService` и др.) |
| User B (Client) | Получатель уведомления |
| User B — SignalRClientService | `SignalRClientService` на устройстве User B |
| User B — ViewModel | `SocialViewModel` / `SharedGroupDetailViewModel` |

### Сценарий 1: Подключение и подписка на группы

1. **User A** успешно авторизуется.
2. **SignalRClientService**: `ConnectAsync(token)`:
   - Создаёт `HubConnectionBuilder()`:
     - `.WithUrl("{apiBaseUrl}/hubs/social", options => { options.AccessTokenProvider = () => Task.FromResult(token); })`.
     - `#if DEBUG`: `HttpMessageHandlerFactory` с `ServerCertificateCustomValidationCallback = (_, _, _, _) => true`.
     - `.WithAutomaticReconnect()`.
   - `RegisterHandlers()` — подписка на 6 событий:
     - `"ReceiveNewFollower"` → `NewFollowerReceived?.Invoke(ProfileSummaryDto)`.
     - `"ReceiveGroupInvite"` → `GroupInviteReceived?.Invoke(SharedGroupDto, ProfileSummaryDto)`.
     - `"ReceiveGroupDeleted"` → `GroupDeletedReceived?.Invoke(Guid)`.
     - `"ReceiveNewGroupMember"` → `NewGroupMemberReceived?.Invoke(Guid, ProfileSummaryDto)`.
     - `"ReceiveTaskCompleted"` → `TaskCompletedReceived?.Invoke(Guid, Guid)`.
     - `"ReceiveNewGroupTask"` → `NewGroupTaskReceived?.Invoke(Guid)`.
   - `_connection.StartAsync()`.
3. **SocialHub** (сервер): `OnConnectedAsync()`:
   - Извлекает `accountId` из JWT claim `sub` (`JwtRegisteredClaimNames.Sub`).
   - `Groups.AddToGroupAsync(Context.ConnectionId, accountId.ToString())` — персональная группа.
4. **SignalRClientService**: для каждой общей группы пользователя: `JoinGroupAsync(groupId)`:
   - → **SocialHub**: `JoinGroup(groupId.ToString())`.
   - **SocialHub**: `Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}")`.

### Сценарий 2: User A выполняет задачу в общей группе → уведомление User B

1. **User A** → `ToggleTask(task)` в `HomeViewModel` → API `POST /api/activities/toggle`.
2. **Server — Business Logic** (`DailyActivityService`): `ToggleCompletionAsync(accountIdA, request)`.
   - Проверяет ownership или membership.
   - Создаёт/удаляет `ActivityCompletion`.
   - Определяет, что группа — shared (`membershipRepo`).
3. **Server — Business Logic** → **SocialRealTimeNotifier**: `SendTaskCompletedAsync(groupId, actorId: accountIdA, itemId)`.
4. **SocialRealTimeNotifier**: `hub.Clients.Group($"group_{groupId}").SendAsync("ReceiveTaskCompleted", actorId, itemId)`.
   - SignalR отправляет сообщение всем соединениям в группе `group_{groupId}`.
5. **User B — SignalRClientService**: handler `ReceiveTaskCompleted` вызывает `TaskCompletedReceived?.Invoke(actorId, itemId)`.
6. **User B — ViewModel** (подписчик события): обновляет UI (прогресс участника группы).

### Сценарий 3: User A отправляет приглашение в группу → уведомление User B

1. **User A** → API `POST /api/social/groups/{groupId}/invite`.
2. **Server — Business Logic** (`SocialService`): создаёт `GroupMembership` с `Status = Pending`.
3. → **SocialRealTimeNotifier**: `SendGroupInviteAsync(recipientId: accountIdB, SharedGroupDto, inviter: ProfileSummaryDto)`.
4. **SocialRealTimeNotifier**: `hub.Clients.Group(accountIdB.ToString()).SendAsync("ReceiveGroupInvite", group, inviter)`.
5. **User B — SignalRClientService**: `GroupInviteReceived?.Invoke(group, inviter)`.
6. **User B — ViewModel**: показывает уведомление о приглашении.

### Сценарий 4: User A подписывается на User B → уведомление User B

1. **User A** → API `POST /api/social/follow/{targetId}`.
2. **Server — Business Logic** (`SocialService`): создаёт `Friendship` с `Status = Accepted`.
3. → **SocialRealTimeNotifier**: `SendNewFollowerAsync(recipientId: targetId, follower: ProfileSummaryDto)`.
4. **SocialRealTimeNotifier**: `hub.Clients.Group(targetId.ToString()).SendAsync("ReceiveNewFollower", follower)`.
5. **User B — SignalRClientService**: `NewFollowerReceived?.Invoke(follower)`.

### Сценарий 5: Отключение

1. **SignalRClientService**: `DisconnectAsync()`:
   - `_connection.DisposeAsync()`, `_connection = null`.
2. **SocialHub**: `OnDisconnectedAsync()`:
   - `Groups.RemoveFromGroupAsync(Context.ConnectionId, accountId.ToString())`.

### Полная таблица серверных событий

| Событие | Метод `SocialRealTimeNotifier` | SignalR-метод | Routing | Payload |
|---------|-------------------------------|--------------|---------|---------|
| Новый подписчик | `SendNewFollowerAsync` | `ReceiveNewFollower` | `recipientId` (персональная) | `ProfileSummaryDto` |
| Приглашение в группу | `SendGroupInviteAsync` | `ReceiveGroupInvite` | `recipientId` (персональная) | `SharedGroupDto`, `ProfileSummaryDto` |
| Группа удалена | `SendGroupDeletedAsync` | `ReceiveGroupDeleted` | `recipientId` (персональная) | `Guid groupId` |
| Новый участник группы | `SendNewGroupMemberAsync` | `ReceiveNewGroupMember` | `recipientId` (персональная) | `Guid groupId`, `ProfileSummaryDto` |
| Задача выполнена | `SendTaskCompletedAsync` | `ReceiveTaskCompleted` | `group_{groupId}` (групповая) | `Guid actorId`, `Guid itemId` |
| Новая задача в группе | `SendNewGroupTaskAsync` | `ReceiveNewGroupTask` | `group_{groupId}` (групповая) | `Guid itemId` |

### Ключевые детали для отрисовки

- **Два типа SignalR-групп**: персональная (`accountId.ToString()`) и групповая (`group_{groupId}`). Показать как аннотацию.
- **`[Authorize]`** на `SocialHub` — JWT-аутентификация при WebSocket handshake.
- **`WithAutomaticReconnect()`** — показать как note.
- **`IHubContext<SocialHub>`** в `SocialRealTimeNotifier` — сервис использует hub context, а не сам Hub. Это позволяет пушить сообщения из любого сервиса, а не только из Hub.
- Направление стрелок: Server → Client (push), Client → Server (только `JoinGroup`/`LeaveGroup`).

---

## 7. AI Chat с контекстом пользователя — RAG-lite архитектура

**Тип диаграммы:** Activity Diagram

### Акторы / компоненты

| Компонент | Проект | Роль |
|-----------|--------|------|
| `AiChatViewModel` | Client.Application | UI чата |
| `ChatApiService` | Client.Infrastructure | HTTP-клиент |
| `ChatController` | Server.Api | API-контроллер |
| `ChatService` | Server.Application | Оркестрация: лимиты, контекст, Gemini |
| `SubscriptionService` | Server.Application | Проверка Premium-статуса |
| `ChatRepository` | Server.Infrastructure | Хранение разговоров и сообщений |
| `StatsRepository` | Server.Infrastructure | Получение screen time и usage records |
| `DailyActivityService` | Server.Application | Получение групп и задач |
| `IRepository<BlockRule>` | Server.Infrastructure | Получение правил блокировки |
| Gemini API | Google Gemini 2.5 Flash | Генерация ответа |

### Полный поток (Activity Diagram)

1. **Start**: User отправляет сообщение в чат.
2. **Action**: `AiChatViewModel` → `ChatApiService.SendMessageAsync(conversationId?, message, todayContext)` → `POST /api/chat/send`.
   - `TodayLocalContext` — собирается на клиенте: `TotalScreenTimeSeconds`, `Pickups`, `TopApps[]` (данные с устройства, ещё не синхронизированные на сервер).

---

**Fork** (параллельные проверки):

---

#### Ветка A: Проверка лимита сообщений

3. **Action**: `SubscriptionService.IsPremiumAsync(accountId)`:
   - Загружает `Subscription` по `accountId`.
   - Проверяет `Status == Active` и `CurrentPeriodEnd > UtcNow`.
4. **Decision**: `isPremium`?
   - [Да] → пропуск проверки лимита.
   - [Нет] → `chatRepository.CountTodayUserMessagesAsync(accountId)`:
     - **Decision**: `todayCount >= freeLimitsSettings.MaxDailyChatMessages`?
       - [Да] → **throw `ChatLimitExceededException(limit)`** → **End** (ошибка 429).
       - [Нет] → продолжение.

#### Ветка B: Управление разговором

5. **Decision**: `conversationId.HasValue`?
   - [Да] → `chatRepository.GetConversationWithMessagesAsync(conversationId, accountId)`.
     - [null] → **throw `InvalidOperationException("Conversation not found")`** → **End**.
   - [Нет] → создание нового: `new ChatConversation { AccountId, Title = message[..50] + "...", CreatedAt = UtcNow }` → `chatRepository.CreateConversationAsync`.

---

**Join** (после обеих веток):

---

6. **Action**: сохранение сообщения пользователя: `new ChatMessage { ConversationId, Role = "user", Content = message, CreatedAt = UtcNow }` → `chatRepository.AddMessageAsync`.

---

**Fork** (параллельный сбор контекста):

---

#### Сбор контекста: BuildSystemPromptAsync

7. **Action**: базовый system prompt — статический текст:
   ```
   "You are Bloomdo AI — a friendly, supportive personal productivity assistant..."
   "Respond in the same language the user writes to you."
   ```

8. **Action** (параллельно):
   - **8a**: `statsRepository.GetSnapshotsForMonthAsync(accountId, weekStart, today)` → секция `=== USER'S RECENT SCREEN TIME DATA ===`:
     - Для каждого дня: `{date}: {hours}h {mins}m screen time, {pickups} pickups, goal met: {goalMet}`.
   - **8b**: инъекция `TodayLocalContext` (если предоставлен клиентом) → секция `=== TODAY'S LIVE DATA (from device) ===`:
     - `Screen time so far: Xh Ym`, `Pickups so far: N`, Top apps с расшифровкой времени.
   - **8c**: `statsRepository.GetUsageRecordsForRangeAsync(accountId, weekStart, today)` → секция `=== TOP APPS THIS WEEK ===`:
     - GroupBy `PackageName`, Sum `ForegroundSeconds`, OrderByDescending, Take(5).
   - **8d**: `activityService.GetGroupsAsync(accountId)` (try/catch, безопасный) → секция `=== USER'S ACTIVITY GROUPS & TASKS ===`:
     - Для каждой группы: `Group: {icon} {title}`, для каждого item: `- {title} (type: {taskType})`.
   - **8e**: `blockRuleRepository.FindAsync(b => !b.IsDeleted)` + фильтрация по `accountId` → секция `=== USER'S APP BLOCKING RULES ===`:
     - Для каждого правила: `Rule: {title} (type: {type}, active: {isActive})`.
   - **8f**: `statsRepository.GetGoalMetDatesAsync(accountId)` → секция `=== STREAKS ===`:
     - Подсчёт текущего streak (последовательные дни с goalMet от сегодня назад).
     - `Current streak: N days`, `Total goal-met days: M`.

---

**Join** (все данные контекста собраны):

---

9. **Action**: конкатенация всех секций в единый `systemPrompt` (StringBuilder).

10. **Action**: подготовка истории:
    - `conversation.Messages.Where(!IsDeleted).OrderBy(CreatedAt).TakeLast(50)` — ограничение до 50 последних сообщений (`MaxHistoryMessages`).

11. **Action**: `CallGeminiAsync(systemPrompt, history, newMessage)`:
    - Преобразование `history` в `List<Content>`:
      - `msg.Role == "assistant"` → `Role = "model"`.
      - `msg.Role == "user"` → `Role = "user"`.
    - Добавление нового сообщения пользователя как последнего `Content`.
    - `GenerateContentConfig { SystemInstruction = new Content { Parts = [systemPrompt] }, Temperature = 0.7f, MaxOutputTokens = 2048 }`.
    - **Цикл по API-ключам** (`geminiSettings.ApiKeys`):
      - `var client = new Client(apiKey: apiKeys[i])`.
      - → **Gemini API**: `client.Models.GenerateContentAsync(model: "gemini-2.5-flash", contents, config)`.
      - [alt: успех] → извлекает `response.Candidates[0].Content.Parts[0].Text`. Выход из цикла.
      - [alt: `ClientError` и `i < apiKeys.Count - 1`] → переход к следующему ключу.
    - Если все ключи исчерпаны → возвращает `"⚠️ AI assistant is temporarily unavailable (all API keys exhausted). Please try again later."` (graceful degradation, не exception).

12. **Action**: сохранение ответа ассистента: `new ChatMessage { ConversationId, Role = "assistant", Content = aiResponseText, CreatedAt = UtcNow }` → `chatRepository.AddMessageAsync`.

13. **Action**: обновление timestamp разговора: `conversation.UpdatedAt = UtcNow` → `chatRepository.UpdateConversationAsync` → `chatRepository.SaveChangesAsync`.

14. **Action**: формирование ответа: `SendMessageResponse { UserMessage = { Id, Role, Content, CreatedAt }, AssistantMessage = { Id, Role, Content, CreatedAt } }`.

15. **End**: ответ передаётся обратно клиенту.

### Ключевые детали для отрисовки

- **Fork/Join для сбора контекста** (шаги 8a–8f) — 6 параллельных запросов к разным источникам. Это ключевая визуальная деталь, показывающая RAG-lite подход.
- **Decision node** для freemium лимита — чёткое разделение free/premium пользователей.
- **TodayLocalContext** — данные с клиента, ещё не синхронизированные. Показать как входной параметр из другого swimlane (Client).
- **Multi-key fallback** — loop node с graceful degradation (не exception, а fallback-сообщение).
- **Temperature 0.7** для чата vs **0.1** для photo verification — можно показать как note для сравнения.
- **MaxHistoryMessages = 50** — truncation для управления context window.
- **try/catch** вокруг activities и block rules — безопасный сбор (если один источник недоступен, остальные всё равно попадают в промпт).
