# Bloomdo — UML-спецификация для 7 диаграмм

> Документ основан на реальной кодовой базе проекта Bloomdo.  
> Технологический стек: .NET 10, Avalonia UI (MVVM, CommunityToolkit.Mvvm), Android Foreground Service, ASP.NET Core API, EF Core + SQLite (клиент), PostgreSQL (сервер), SignalR, Stripe.

---

## 1. USE CASE DIAGRAM (Диаграмма прецедентов)

### 1.1 Акторы

| Актор | Тип | Описание |
|---|---|---|
| **User (Пользователь)** | Первичный | Владелец устройства, взаимодействует с UI приложения |
| **BlockEnforcementForegroundService** | Системный | Android Foreground Service, работает в фоне независимо от UI; мониторит foreground-приложение каждые 2 секунды |
| **Bloomdo Server API** | Внешняя система | REST API-бэкенд: аутентификация, хранение правил блокировки, синхронизация статистики, AI-чат, социальные функции |
| **UsageSyncService** | Системный | Фоновый сервис синхронизации локального кэша использования с сервером |
| **Android OS (UsageStatsManager)** | Внешняя система | Предоставляет данные о foreground-приложениях и статистике использования |

### 1.2 Варианты использования

#### Группа «Аутентификация и онбординг»
- **UC-01: Регистрация** — User → заполняет email/password → Server API создаёт Account → возвращает JWT + Refresh Token
- **UC-02: Вход** — User → вводит credentials → Server API валидирует → возвращает токены
- **UC-03: Прохождение онбординга** — User → WelcomeStep → AskNameStep → SetGoalsStep → переход на главный экран
- **UC-04: Автообновление токена** — AccessTokenManager → при истечении JWT → вызывает RefreshToken endpoint → обновляет ITokenStorage

#### Группа «Управление блокировками»
- **UC-05: Создание правила блокировки (Schedule)** — User → выбирает приложения из списка установленных → задаёт StartTime, EndTime, Days → сохраняет через BlockApiService → правила кэшируются в block_rules.json через IBlockRuleStore
- **UC-06: Создание правила блокировки (Limit)** — User → выбирает приложения → задаёт DailyLimitMinutes → сохраняет
- **UC-07: Создание правила блокировки (Focus)** — User → выбирает приложения → задаёт FocusDurationMinutes → активирует сессию → FocusStartedAtUtc фиксируется
- **UC-08: Создание правила блокировки (Bloomdo)** — User → выбирает приложения → привязывает к ActivityGroup → блокировка снимается только после выполнения задач группы
- **UC-09: Переключение активности правила** — User → toggle IsActive → обновление на сервере → синхронизация в локальный кэш
- **UC-10: Удаление правила блокировки** — User → подтверждение → удаление на сервере и из кэша

#### Группа «Фоновый мониторинг и блокировка» (основная фича)
- **UC-11: Мониторинг foreground-приложения** — BlockEnforcementForegroundService → каждые 2 сек опрашивает UsageStatsManager → получает текущий foreground package
- **UC-12: Блокировка запрещённого приложения** — ForegroundService → определяет что package в cachedRules → показывает BlockedActivity (полноэкранный блокирующий экран)
- **UC-13: Периодическое сохранение использования** — ForegroundService → каждые 15 минут → SaveUsageLocallyIfDue → пишет в usage_cache/*.json
- **UC-14: Проверка выполнения дневного лимита** — ForegroundService → IsOverDailyLimit → сравнивает TotalTimeInForeground с DailyLimitMinutes
- **UC-15: Проверка завершения группы задач (Bloomdo-тип)** — ForegroundService → загружает _cachedGroupCompletion → если группа завершена — разблокирует

#### Группа «Активности и задачи»
- **UC-16: Просмотр дневных активностей** — User → HomeView → загрузка ActivityGroups с задачами
- **UC-17: Отметка выполнения задачи** — User → toggle completion → ToggleCompletionRequest → сервер создаёт/удаляет ActivityCompletion
- **UC-18: Выполнение задачи с таймером** — User → запускает TimerDialog → TimerStateSnapshot сохраняется в ITimerStateStore → отсчёт → по завершению автоматическая отметка
- **UC-19: Фото-верификация задачи** — User → фотографирует выполнение → PhotoVerificationDialogService → отправка на сервер → AI-верификация
- **UC-20: Создание/редактирование группы активностей** — User → GroupEditorView → создание ActivityGroup с ActivityItem'ами

#### Группа «Статистика и аналитика»
- **UC-21: Просмотр экранного времени за сегодня** — User → StatsView → IAppUsageService.GetTodayUsageAsync → отображение суммарного времени и топ-приложений
- **UC-22: Просмотр недельной статистики** — User → WeeklyStatsResponse с сервера (или из LocalStatsStore при оффлайне) → гистограмма по дням
- **UC-23: Просмотр календаря месяца** — User → MonthCalendarResponse → календарь с отметками GoalMet
- **UC-24: Синхронизация использования на сервер** — UsageSyncService → SaveLocalSnapshotAsync → SyncToServerAsync → отправка SyncUsageRequest

#### Группа «Социальные функции»
- **UC-25: Поиск пользователей** — User → UserSearchView → ISocialApiService
- **UC-26: Подписка/отписка** — User → Follow/Unfollow → IFriendsApiService → Friendship entity
- **UC-27: Просмотр профиля пользователя** — User → UserProfileView → UserProfileDto
- **UC-28: Управление общими группами** — User → SharedGroupEditorView → приглашение участников → GroupMembership

#### Группа «Профиль и настройки»
- **UC-29: Редактирование профиля** — User → ProfileEditorView → UpdateProfileRequest
- **UC-30: Настройка аватара** — User → AvatarEditorView → AvatarConfig → сохранение AvatarJson
- **UC-31: Управление подпиской** — User → SubscriptionView → Stripe Checkout → Subscription entity
- **UC-32: Настройки приложения** — User → SettingsView → тема, уведомления, выход

#### Группа «AI-чат»
- **UC-33: Общение с AI-ассистентом** — User → AiChatView → SendMessageRequest → ChatMessage → AI-ответ с контекстом текущей активности (TodayLocalContext)

### 1.3 Связи между прецедентами

- UC-05..UC-08 **«include»** → UC-11 (все созданные правила автоматически загружаются в ForegroundService)
- UC-17, UC-19 **«include»** → UC-15 (отметка задачи обновляет GroupCompletion → влияет на блокировку Bloomdo-типа)
- UC-21..UC-23 **«include»** → UC-24 (просмотр статистики тригерит синхронизацию)
- UC-12 **«extend»** → UC-14 (для Limit-типа дополнительно проверяется дневной лимит)
- UC-12 **«extend»** → UC-15 (для Bloomdo-типа проверяется завершение группы)

---

## 2. CLASS DIAGRAM (Диаграмма классов)

### 2.1 Слой Domain — Серверные сущности (наследуют BaseEntity)

#### BaseEntity (abstract)
- `Id: Guid` (PK)
- `CreatedAt: DateTime`
- `CreatedBy: string?`
- `UpdatedAt: DateTime?`
- `UpdatedBy: string?`
- `IsDeleted: bool` (soft delete)
- `DeletedAt: DateTime?`
- `DeletedBy: string?`

#### Account : BaseEntity
- `Email: string`
- `PasswordHash: string`
- `FirstName: string?`
- `LastName: string?`
- `Username: string?`
- `Bio: string?`
- `AvatarJson: string?`
- `IsEmailConfirmed: bool`
- `LastLoginAt: DateTime?`
- `ProfileVisibility: ProfileVisibility`
- Навигация: `AccountRoles (1:M)`, `RefreshTokens (1:M)`, `InitiatedFriendships (1:M)`, `ReceivedFriendships (1:M)`, `GroupMemberships (1:M)`

#### BlockRule : BaseEntity
- `AccountId: Guid` (FK → Account)
- `Title: string`
- `Type: BlockType` (enum: Schedule=0, Limit=1, Focus=2, Bloomdo=3)
- `IsActive: bool`
- `BlockedPackagesJson: string` (JSON-массив package names)
- `StartTime: TimeOnly?`, `EndTime: TimeOnly?` (для Schedule)
- `ScheduleDaysJson: string?` (JSON-массив DayOfWeek)
- `DailyLimitMinutes: int?` (для Limit)
- `FocusDurationMinutes: int?`, `FocusStartedAtUtc: DateTime?` (для Focus)
- `RequiredActivityGroupId: Guid?` (FK → ActivityGroup, для Bloomdo)
- Связь: Account → BlockRule = **1:M (композиция)**

#### ActivityGroup : BaseEntity
- `AccountId: Guid` (FK → Account)
- `Title: string`, `Icon: string`, `Color: string`
- `SortOrder: int`, `IsActive: bool`
- Навигация: `Items: ICollection<ActivityItem>` (1:M), `Memberships: ICollection<GroupMembership>` (1:M)
- Связь: Account → ActivityGroup = **1:M (композиция)**

#### ActivityItem : BaseEntity
- `ActivityGroupId: Guid` (FK → ActivityGroup)
- `Title: string`, `Description: string?`
- `TaskType: int`, `DurationMinutes: int?`, `TargetCount: int?`
- `Icon: string`, `Color: string`, `SortOrder: int`, `IsActive: bool`
- `VerificationTemplateId: int?`, `CustomVerificationCriteria: string?`
- Навигация: `Completions: ICollection<ActivityCompletion>` (1:M)
- Связь: ActivityGroup → ActivityItem = **1:M (композиция)**

#### ActivityCompletion : BaseEntity
- `ActivityItemId: Guid` (FK → ActivityItem)
- `AccountId: Guid` (FK → Account)
- `Date: DateOnly`
- `CompletedAtUtc: DateTime`
- `CountValue: int?`, `Note: string?`
- Связь: ActivityItem → ActivityCompletion = **1:M**

#### DailySnapshot : BaseEntity
- `AccountId: Guid` (FK → Account)
- `Date: DateOnly`
- `TotalScreenTimeSeconds: int`, `Pickups: int`, `GoalMet: bool`
- Связь: Account → DailySnapshot = **1:M**

#### AppUsageRecord : BaseEntity
- `AccountId: Guid` (FK → Account)
- `Date: DateOnly`
- `PackageName: string`, `AppLabel: string?`
- `ForegroundSeconds: int`
- Связь: Account → AppUsageRecord = **1:M**

#### Subscription : BaseEntity
- `AccountId: Guid` (FK → Account)
- `StripeCustomerId: string?`, `StripeSubscriptionId: string?`
- `Plan: SubscriptionPlan`, `Status: SubscriptionStatus`
- `CurrentPeriodStart: DateTime`, `CurrentPeriodEnd: DateTime`
- `CancelAtPeriodEnd: bool`, `CancelledAt: DateTime?`
- Связь: Account → Subscription = **1:1**

#### Achievement : BaseEntity
- `Code: string`, `Title: string`, `Description: string`, `Icon: string`, `SortOrder: int`
- Навигация: `AccountAchievements: ICollection<AccountAchievement>`

#### AccountAchievement : BaseEntity
- `AccountId: Guid`, `AchievementId: Guid`, `UnlockedDate: DateOnly`
- Связь: Account ↔ Achievement = **M:M** (через AccountAchievement)

#### Friendship : BaseEntity
- `RequesterId: Guid`, `AddresseeId: Guid`
- `Status: FriendshipStatus` (Pending, Accepted, Declined, Blocked)
- Связь: Account ↔ Account = **M:M** (через Friendship; self-referencing)

#### GroupMembership : BaseEntity
- `ActivityGroupId: Guid`, `AccountId: Guid`
- `Role: GroupMemberRole`, `Status: GroupMemberStatus`
- Связь: Account ↔ ActivityGroup = **M:M** (через GroupMembership)

#### Notification : BaseEntity
- `RecipientId: Guid`, `ActorId: Guid?`
- `Type: NotificationType`, `ReferenceId: Guid?`, `IsRead: bool`

#### ChatConversation : BaseEntity
- `AccountId: Guid`, `Title: string`
- Навигация: `Messages: ICollection<ChatMessage>` (1:M)

#### ChatMessage : BaseEntity
- `ConversationId: Guid`, `Role: string` ("user"/"assistant"), `Content: string`

#### StreakFreeze : BaseEntity
- `AccountId: Guid`, `Date: DateOnly`
- Связь: Account → StreakFreeze = **1:M**

### 2.2 Слой Domain — Клиентские модели

#### AppUsageInfo (sealed)
- `PackageName: string`, `AppLabel: string?`, `ForegroundTime: TimeSpan`

#### InstalledAppInfo (sealed)
- `PackageName: string`, `AppLabel: string`

#### LocalUsageSnapshot (sealed)
- `Date: DateOnly`, `LastUpdatedUtc: DateTime`, `Pickups: int`, `SyncedToServer: bool`
- `Apps: List<LocalAppUsageEntry>` (1:M, **композиция**)

#### LocalAppUsageEntry (sealed)
- `PackageName: string`, `AppLabel: string?`, `ForegroundSeconds: int`

#### LocalProfileSnapshot (sealed)
- `LastUpdatedUtc: DateTime`
- `Name, Email, Username, Bio, Initials, JoinedDateText: string`
- `Avatar: AvatarConfig?`
- `FollowersCount, FollowingCount: int`
- `StreakDays, TasksCompleted, FocusHours, TotalBlocksCreated, AchievementsUnlocked: int`
- `Level: string`, `IsPremium: bool`

#### TimerStateSnapshot
- `TaskId: Guid`, `TaskTitle: string`, `TaskIcon: string`, `TaskColor: string`
- `TotalSeconds, RemainingSeconds, DurationMinutes, Streak: int`
- `IsRunning, IsPaused: bool`
- `LastTickUtc: DateTime`, `Date: DateOnly`

### 2.3 Слой Core — Интерфейсы сервисов

| Интерфейс | Ключевые методы |
|---|---|
| `IBlockEnforcementService` | `Start()`, `Stop()` |
| `IBlockRuleStore` | `SaveRulesAsync(rules)`, `LoadRulesAsync()` |
| `IBlockApiService` | `CreateBlockRuleAsync(request)`, `UpdateBlockRuleAsync(id, request)`, `DeleteBlockRuleAsync(id)`, `GetBlockRulesAsync()` |
| `IAppUsageService` | `GetTodayUsageAsync()`, `GetPickupsTodayAsync()` |
| `IInstalledAppsService` | `GetInstalledAppsAsync()` |
| `ILocalUsageStore` | `SaveSnapshotAsync(snapshot)`, `LoadSnapshotAsync(date)`, `GetUnsyncedSnapshotsAsync()`, `MarkSyncedAsync(date)` |
| `IUsageSyncService` | `SaveLocalSnapshotAsync()`, `SyncToServerAsync()`, `SyncPendingAsync()` |
| `IStatsApiService` | `SyncUsageAsync(request)`, `GetWeeklyAsync(start)`, `GetDailyAsync(date)`, `GetMonthCalendarAsync(year, month)` |
| `ILocalStatsStore` | `SaveMonthCalendarAsync(...)`, `LoadMonthCalendarAsync(...)`, `SaveWeeklyStatsAsync(...)`, `LoadWeeklyStatsAsync(...)`, `SaveDailyStatsAsync(...)`, `LoadDailyStatsAsync(...)`, `CleanupAsync(days)` |
| `IAuthApiService` | `LoginAsync(request)`, `RegisterAsync(request)`, `RefreshTokenAsync(request)` |
| `IAuthService` | (клиентский сервис авторизации) |
| `ITokenStorage` | `SaveTokensAsync(...)`, `GetAccessTokenAsync()`, `GetRefreshTokenAsync()`, `ClearAsync()` |
| `IAuthorizationService` | `CheckAccessAsync(policy)` |
| `INavigationService` | `NavigateToAsync<T>(...)`, `GoBackAsync()` |
| `ITimerDialogService` | `ShowTimerAsync(...)` |
| `ITimerStateStore` | `SaveAsync(snapshot)`, `LoadAsync()`, `ClearAsync()` |
| `IGroupCompletionStore` | `SaveAsync(completions)`, `LoadAsync()` |
| `ILocalProfileStore` | `SaveAsync(snapshot)`, `LoadAsync()` |
| `IConnectivityService` | `IsOnline: bool` |
| `IAppIconProvider` | `GetIconAsync(packageName)` |
| `IToastService` | `Show(message, type)` |
| `IConfirmDialogService` | `ShowAsync(title, message)` |
| `IPhotoVerificationDialogService` | `ShowAsync(...)` |
| `IPreferencesService` | `Get<T>(key)`, `Set<T>(key, value)` |
| `ILocalSubscriptionStore` | `SaveAsync(status)`, `LoadAsync()` |
| `ILocalActivityCache` | `SaveDailyAsync(daily, date)`, `LoadDailyAsync(date)` |
| `IDailyActivityApiService` | `GetDailyAsync()`, `ToggleCompletionAsync(request)`, `VerifyPhotoAsync(request)` |
| `ISignalRClientService` | `ConnectAsync()`, `DisconnectAsync()` |
| `IFriendsApiService` | `FollowAsync(userId)`, `UnfollowAsync(userId)`, `GetFriendsAsync()` |
| `ISocialApiService` | `SearchUsersAsync(query)`, `GetUserProfileAsync(userId)` |
| `IProfileApiService` | `GetProfileAsync()`, `UpdateProfileAsync(request)` |
| `IChatApiService` | `SendMessageAsync(request)`, `GetConversationsAsync()`, `GetConversationAsync(id)` |
| `ISubscriptionApiService` | `GetStatusAsync()`, `CreateCheckoutSessionAsync(request)` |
| `IBrowserService` | `OpenAsync(url)` |
| `IImagePickerService` | `PickImageAsync()` |

### 2.4 Слой Application — ViewModels

| ViewModel | Зависимости (через constructor injection) | Описание |
|---|---|---|
| `ShellViewModel` | `INavigationService`, `IAuthorizationService` | Корневая оболочка, содержит стек навигации |
| `MainViewModel` | — | Контейнер нижних табов (Home, Blocks, Stats, Social, Profile) |
| `HomeViewModel` | `IDailyActivityApiService`, `IGroupCompletionStore`, `IBlockRuleStore`, `IBlockApiService`, `INavigationService`, `ITimerDialogService`, `IConfirmDialogService`, `IPhotoVerificationDialogService`, `IToastService`, `IConnectivityService`, `ILocalActivityCache` | Главный экран: дневные группы задач |
| `BlocksViewModel` | `IBlockApiService`, `IInstalledAppsService`, `IBlockRuleStore`, `IDailyActivityApiService`, `IAppIconProvider`, `ISubscriptionApiService`, `IConnectivityService`, `ILocalSubscriptionStore` | Список правил блокировки |
| `BlockEditorViewModel` | `IBlockApiService`, `IInstalledAppsService`, `IDailyActivityApiService`, `IAppIconProvider` | Создание/редактирование правила |
| `StatsViewModel` | `IAppUsageService`, `IStatsApiService`, `IAppIconProvider`, `ISubscriptionApiService`, `IUsageSyncService`, `IConnectivityService`, `ILocalStatsStore`, `ILocalSubscriptionStore` | Статистика: экранное время, пикапы, календарь, недельный график |
| `ProfileViewModel` | `IProfileApiService`, `INavigationService`, `ILocalProfileStore`, `IConnectivityService` | Профиль пользователя |
| `SocialViewModel` | `ISocialApiService`, `IFriendsApiService`, `INavigationService` | Социальная лента |
| `AiChatViewModel` | `IChatApiService` | AI-чат ассистент |
| `TimerDialogViewModel` | `ITimerStateStore` | Модальный таймер для задач |
| `LoginViewModel` | `IAuthApiService`, `ITokenStorage`, `INavigationService` | Экран входа |
| `RegisterViewModel` | `IAuthApiService`, `ITokenStorage`, `INavigationService` | Экран регистрации |
| `SettingsViewModel` | `IThemeService`, `IPreferencesService`, `IAuthService`, `INavigationService` | Настройки |

### 2.5 Слой Android — Platform-specific

| Класс | Связи |
|---|---|
| `BlockEnforcementForegroundService : Android.App.Service` | Напрямую читает `block_rules.json` и `group_completion.json`; использует `UsageStatsManager` (Android OS); запускает `BlockedActivity` |
| `AndroidBlockEnforcementService : IBlockEnforcementService` | Обёртка — создаёт Intent → `StartForegroundService`/`StopService`; **реализует** интерфейс Core-слоя |
| `AndroidAppUsageService : IAppUsageService` | Реализует через `UsageStatsManager` |
| `AndroidInstalledAppsService : IInstalledAppsService` | Реализует через `PackageManager` |
| `AndroidAppIconProvider : IAppIconProvider` | Реализует через `PackageManager` |
| `BlockedActivity : Android.App.Activity` | Полноэкранный блокирующий UI; кнопка "Go Back" → Intent(ACTION_MAIN, CATEGORY_HOME) |
| `MainActivity : MauiAppCompatActivity` | Точка входа Android-приложения |

### 2.6 Ключевые типы связей между слоями

- **Core** (интерфейсы) ← **реализуется** ← **Infrastructure** (конкретные реализации: API-клиенты, файловые хранилища)
- **Core** (интерфейсы) ← **реализуется** ← **Android** (платформенные реализации: UsageStats, InstalledApps, ForegroundService)
- **Application** (ViewModels) → **зависит от** → **Core** (интерфейсы) — через constructor DI
- **Application** (ViewModels) → **зависит от** → **Domain** (модели) и **Shared** (DTOs, Enums)
- **UI** (Views) → **привязка** → **Application** (ViewModels) — DataBinding Avalonia AXAML
- **Startup** (DependencyContainer) → **собирает** → все слои через `IServiceCollection`

---

## 3. COMPONENT DIAGRAM (Диаграмма компонентов)

### 3.1 Компоненты

#### «Bloomdo.Client.UI» — Презентационный слой
- **Технология**: Avalonia UI, AXAML Views
- **Содержит**: Views (ShellView, MainView, HomeView, BlocksView, StatsView, etc.), Converters, Controls (SwipeRevealPanel), Services (ToastService, ThemeService, ConfirmDialogService, TimerDialogService, PhotoVerificationDialogService, AvaloniaImagePickerService)
- **Предоставляемый интерфейс**: DataBinding (INotifyPropertyChanged) к ViewModels
- **Зависит от**: Bloomdo.Client.Application (ViewModels), ShadUI (UI-библиотека)

#### «Bloomdo.Client.Application» — Слой приложения
- **Технология**: CommunityToolkit.Mvvm, ObservableObject, RelayCommand
- **Содержит**: ViewModels (все *ViewModel классы), Services (NavigationService, AuthorizationService, UsageSyncService), Helpers (AvatarColorHelper)
- **Предоставляемый интерфейс**: ViewModels для привязки
- **Зависит от**: Bloomdo.Client.Core (интерфейсы), Bloomdo.Client.Domain (модели), Bloomdo.Shared (DTOs)

#### «Bloomdo.Client.Core» — Ядро контрактов
- **Содержит**: все интерфейсы `I*Service`, `I*Store`, `I*ApiService`; AppConfig
- **Предоставляемый интерфейс**: контракты для DI
- **Зависит от**: Bloomdo.Client.Domain (модели), Bloomdo.Shared (DTOs)

#### «Bloomdo.Client.Domain» — Доменные модели клиента
- **Содержит**: AppUsageInfo, InstalledAppInfo, LocalUsageSnapshot, LocalAppUsageEntry, LocalProfileSnapshot, TimerStateSnapshot, AuthorizationResult, Enums (AuthorizationFailureType, AuthorizationPolicy, ToastType), Attributes (AuthorizeAttribute)
- **Зависимости**: Bloomdo.Shared (для AvatarConfig DTO)

#### «Bloomdo.Client.Infrastructure» — Инфраструктура клиента
- **Содержит**:
  - **API-клиенты** (AuthApiService, BlockApiService, StatsApiService, DailyActivityApiService, ProfileApiService, SocialApiService, FriendsApiService, ChatApiService, SubscriptionApiService) — HttpClient + JSON
  - **Локальные хранилища** (BlockRuleStore, LocalUsageStore, LocalStatsStore, LocalProfileStore, LocalSubscriptionStore, GroupCompletionStore, LocalActivityCache, LocalTimerStateStore) — файловая система JSON
  - **Сервисы** (TokenStorage, AccessTokenManager, PreferencesService, ConnectivityService, BrowserService, SignalRClientService)
  - **Middleware** (AuthHeaderHandler — DelegatingHandler для JWT)
  - **DatabaseContexts** (LocalDatabaseContext — EF Core SQLite)
- **Реализует**: интерфейсы из Bloomdo.Client.Core
- **Зависит от**: Bloomdo.Client.Core, Bloomdo.Shared, Microsoft.EntityFrameworkCore, Microsoft.Maui.Storage

#### «Bloomdo.Client.Android» — Платформенный Android-слой
- **Содержит**:
  - `BlockEnforcementForegroundService` (Android Service) — ядро фоновой блокировки
  - `AndroidBlockEnforcementService` (IBlockEnforcementService adapter)
  - `AndroidAppUsageService` (IAppUsageService)
  - `AndroidInstalledAppsService` (IInstalledAppsService)
  - `AndroidAppIconProvider` (IAppIconProvider)
  - `BlockedActivity` (полноэкранный блокирующий Activity)
  - `MainActivity` (точка входа)
- **Реализует**: платформенные интерфейсы из Core
- **Зависит от**: Bloomdo.Client.Core, Bloomdo.Client.Infrastructure (для LocalUsageStore.SaveSnapshotDirect), Android SDK (UsageStatsManager, PackageManager, NotificationManager)

#### «Bloomdo.Client.Startup» — Композиция
- **Содержит**: DependencyContainer — конфигурация всего DI-контейнера
- **Зависит от**: все клиентские модули
- **Предоставляет**: `IServiceProvider`

#### «Bloomdo.Shared» — Общие контракты
- **Содержит**: DTOs (Auth, Blocks, Activities, Stats, Profile, Social, Subscription, Chat, Friends, Achievements), Enums (BlockType, FriendshipStatus, etc.), Constants (ApiRoutes, AppClaimTypes, Permissions)
- **Используется**: и клиентом, и сервером

#### «Bloomdo.Server.Api» — REST API сервер
- **Содержит**: Controllers (AuthController, и др.), Program.cs, Extensions (ServiceCollectionExtensions)
- **Зависит от**: Bloomdo.Server.Application, Bloomdo.Server.Domain, Bloomdo.Server.Infrastructure

#### «Bloomdo.Server.Application» — Серверная бизнес-логика
- **Содержит**: Services (AuthService, и др.), Interfaces (IAuthService и др.)

#### «Bloomdo.Server.Domain» — Серверные сущности
- **Содержит**: Entities (Account, BlockRule, ActivityGroup, ActivityItem, ActivityCompletion, DailySnapshot, AppUsageRecord, Subscription, Achievement, AccountAchievement, Friendship, GroupMembership, Notification, ChatConversation, ChatMessage, StreakFreeze, RefreshToken, Role, RolePermission, AccountRole, BaseEntity), Exceptions

#### «Bloomdo.Server.Infrastructure» — Серверная инфраструктура
- **Содержит**: EF Core DbContext, Repositories, Data seeder (DevDataSeeder)

### 3.2 Интерфейсы взаимодействия между компонентами

```
[Client.UI] ──DataBinding──→ [Client.Application]
[Client.Application] ──DI (интерфейсы)──→ [Client.Core]
[Client.Infrastructure] ──реализует──→ [Client.Core]
[Client.Android] ──реализует──→ [Client.Core]
[Client.Android] ──прямой доступ (файлы JSON)──→ [Client.Infrastructure]
[Client.Infrastructure] ──HTTP/REST──→ [Server.Api]
[Client.Infrastructure] ──SignalR──→ [Server.Api]
[Client.Startup] ──DI composition──→ [все клиентские модули]
[Server.Api] ──→ [Server.Application] ──→ [Server.Domain]
[Server.Infrastructure] ──EF Core──→ [Server.Domain]
[Shared] ←── используется обоими сторонами
```

---

## 4. SEQUENCE DIAGRAM — Процесс блокировки приложения

### Участники (объекты)

1. **:User** — пользователь устройства
2. **:AndroidOS** — операционная система (UsageStatsManager)
3. **:BlockEnforcementForegroundService** — фоновый сервис (тикает каждые 2 сек)
4. **:FileSystem** — `block_rules.json` и `group_completion.json`
5. **:BlockedActivity** — полноэкранный Activity-блокировщик

### Пошаговая последовательность

**Предусловие**: ForegroundService запущен, правила загружены, таймер тикает каждые 2 сек.

```
1.  :Timer → :BlockEnforcementForegroundService  : OnTick(state) вызывается по таймеру (каждые 2 сек)

2.  :BlockEnforcementForegroundService → :FileSystem  : LoadRules()
    Читает block_rules.json из AppDataDirectory.
    :FileSystem → :BlockEnforcementForegroundService  : return List<BlockRuleResponse> → сохраняется в _cachedRules

3.  :BlockEnforcementForegroundService → :FileSystem  : LoadGroupCompletion()
    Читает group_completion.json.
    :FileSystem → :BlockEnforcementForegroundService  : return Dictionary<Guid, bool> → _cachedGroupCompletion

4.  [alt] _cachedRules.Count == 0 :
        return (прекращаем тик — нет правил)

5.  :BlockEnforcementForegroundService → :AndroidOS  : GetForegroundPackage()
    Вызывает UsageStatsManager.QueryEvents(now-30min, now).
    Перебирает все события: ActivityResumed → устанавливает currentForeground;
    ActivityPaused → сбрасывает, если совпадает.
    :AndroidOS → :BlockEnforcementForegroundService  : return currentForeground ("com.instagram.android")

6.  [alt] foregroundPkg == null :
        return (нет foreground-приложения)

7.  [alt] foregroundPkg == OwnPackage ("com.CompanyName.Bloomdo.Client") :
        return (никогда не блокируем себя)

8.  :BlockEnforcementForegroundService → :BlockEnforcementForegroundService  : ShouldBlock(foregroundPkg)
    Итерируется по _cachedRules:

    8a. [loop] для каждого rule в _cachedRules:
        [alt] !rule.IsActive :
            continue (пропускаем неактивные)
        [alt] !rule.BlockedPackages.Contains(packageName) :
            continue (пакет не в списке)

        [alt] rule.Type == Schedule :
            Проверяет IsInSchedule(rule, currentTime, today):
            - Проверка rule.Days содержит текущий DayOfWeek
            - Проверка currentTime между StartTime и EndTime (включая overnight)
            - Если true → return true (БЛОКИРОВАТЬ)

        [alt] rule.Type == Limit :
            Вызывает IsOverDailyLimit(packageName, rule.DailyLimitMinutes):
            8b. :BlockEnforcementForegroundService → :AndroidOS  : UsageStatsManager.QueryAndAggregateUsageStats(startOfDay, now)
                :AndroidOS → :BlockEnforcementForegroundService  : return stats[packageName].TotalTimeInForeground
                Сравнивает usedMinutes >= limitMinutes
            - Если true → return true (БЛОКИРОВАТЬ)

        [alt] rule.Type == Focus :
            Вычисляет elapsed = utcNow - FocusStartedAtUtc
            - Если elapsed < FocusDurationMinutes → return true (БЛОКИРОВАТЬ — сессия активна)

        [alt] rule.Type == Bloomdo :
            Проверяет _cachedGroupCompletion[RequiredActivityGroupId]
            - Если группа завершена (true) → break (разрешить доступ)
            - Иначе → return true (БЛОКИРОВАТЬ — задачи не выполнены)

9.  [alt] ShouldBlock == true AND _lastBlockedPackage != foregroundPkg :
        :BlockEnforcementForegroundService → :BlockEnforcementForegroundService  : _lastBlockedPackage = foregroundPkg

10. :BlockEnforcementForegroundService → :BlockedActivity  : ShowBlockedScreen()
    Создаёт Intent(this, typeof(BlockedActivity)) с флагами NewTask | ClearTop.
    Запускает StartActivity(intent).

11. :BlockedActivity → :User  : Отображает полноэкранный блокирующий экран
    "🔒 App Blocked" + "This app is currently blocked by Bloomdo" + кнопка "Go Back"

12. [alt] User нажимает "Go Back" :
        :User → :BlockedActivity  : Click → Go Back
        :BlockedActivity → :AndroidOS  : Intent(ACTION_MAIN, CATEGORY_HOME) → возврат на домашний экран
        :BlockedActivity  : Finish()

13. [alt] User нажимает системную кнопку Back :
        :BlockedActivity → :AndroidOS  : OnBackPressed() → Intent(ACTION_MAIN, CATEGORY_HOME)

14. :BlockEnforcementForegroundService → :BlockEnforcementForegroundService  : SaveUsageLocallyIfDue()
    [alt] DateTime.UtcNow - _lastUsageSaveUtc >= 15 минут :
        Собирает все usage stats за сегодня через UsageStatsManager.
        Фильтрует только приложения с LaunchIntent.
        Записывает через LocalUsageStore.SaveSnapshotDirect(today, 0, apps).
```

### Альтернативный сценарий: приложение не заблокировано

```
8.  ShouldBlock() returns false
9.  _lastBlockedPackage = null (сброс — следующее блокированное приложение покажет экран)
```

---

## 5. ACTIVITY DIAGRAM — Создание расписания (правила блокировки)

### Процесс: Пользователь создаёт новое правило ограничения

```
[Start]
   │
   ▼
(1) Пользователь открывает экран Blocks (BlocksView → BlocksViewModel.OnAppearing)
   │
   ▼
(2) Система загружает текущие правила: LoadBlockRulesAsync()
    ├─→ [online] BlockApiService.GetBlockRulesAsync() → кэширование через IBlockRuleStore.SaveRulesAsync()
    └─→ [offline] IBlockRuleStore.LoadRulesAsync() из локального кэша
   │
   ▼
(3) Система загружает лимиты подписки: LoadSubscriptionLimitsAsync()
    → SubscriptionApiService.GetStatusAsync() → MaxBlockRules, IsPremium
   │
   ▼
(4) Пользователь нажимает "+" (FAB-кнопку)
   │
   ▼
(5) Открывается всплывающее меню с типами: [IsMenuOpen = true]
    ├── "Focus Session"  (BlockType.Focus)
    ├── "Schedule Block"  (BlockType.Schedule)
    ├── "Daily Limit"  (BlockType.Limit)
    └── "Bloomdo Block"  (BlockType.Bloomdo)
   │
   ▼
(6) ◆ РЕШЕНИЕ: Проверка IsLimitReached
    ├── [true] → Показать уведомление "Upgrade to Premium" → [End]
    └── [false] → продолжить
   │
   ▼
(7) Пользователь выбирает тип блокировки → BlocksViewModel.CreateScheduleBlock/CreateLimitBlock/CreateFocusBlock
    → OpenEditor(BlockType, defaultTitle) → создаётся BlockEditorViewModel
   │
   ▼
(8) BlockEditorViewModel.Configure(type, defaultTitle)
    ├── Устанавливает SelectedType
    ├── Устанавливает BlockTitle
    └── Запускает LoadAppsAsync()
   │
   ▼
(9) LoadAppsAsync():
    → IInstalledAppsService.GetInstalledAppsAsync() → список InstalledAppInfo
    → Для каждого приложения: IAppIconProvider.GetIconAsync(packageName)
    → Формирует List<SelectableAppItem> → отображает в FilteredApps
   │
   ▼
(10) ◆ РЕШЕНИЕ: SelectedType == Bloomdo?
    ├── [true] → LoadGroupsAsync() → IDailyActivityApiService → список ActivityGroupResponse → AvailableGroups
    └── [false] → пропустить
   │
   ▼
(11) Пользователь заполняет форму:

    ◆ ВЕТВЛЕНИЕ по SelectedType:

    ├── [Schedule]:
    │   ├── Выбирает StartTime (TimeSpan, по умолчанию 22:00)
    │   ├── Выбирает EndTime (TimeSpan, по умолчанию 07:00)
    │   ├── Выбирает дни недели (Monday..Sunday, checkboxes)
    │   └── Выбирает приложения из списка (toggle SelectableAppItem.IsSelected)
    │
    ├── [Limit]:
    │   ├── Задаёт DailyLimitMinutes (decimal, по умолчанию 90)
    │   └── Выбирает приложения
    │
    ├── [Focus]:
    │   ├── Задаёт FocusDurationMinutes (decimal, по умолчанию 60)
    │   └── Выбирает приложения
    │
    └── [Bloomdo]:
        ├── Выбирает ActivityGroup из AvailableGroups (SelectedGroup)
        └── Выбирает приложения

(12) Пользователь может использовать поиск:
    → OnSearchTextChanged → ApplyFilter() → фильтрация FilteredApps по SearchText

   │
   ▼
(13) Пользователь нажимает "Save" → BlockEditorViewModel.Save()
   │
   ▼
(14) ◆ ВАЛИДАЦИЯ:
    ├── [BlockTitle пуст] → ErrorMessage = "Enter a block name" → [End - остаёмся в редакторе]
    ├── [0 выбранных приложений] → ErrorMessage = "Select at least one app" → [End]
    ├── [Bloomdo && SelectedGroup == null] → ErrorMessage = "Select an activity group" → [End]
    └── [валидация OK] → продолжить
   │
   ▼
(15) IsSaving = true
   │
   ▼
(16) BuildRequest() → формирует CreateBlockRuleRequest:
    ├── Title, Type, BlockedPackages (список package names выбранных приложений)
    ├── [Schedule] → StartTime, EndTime, Days
    ├── [Limit] → DailyLimitMinutes
    ├── [Focus] → FocusDurationMinutes
    └── [Bloomdo] → RequiredActivityGroupId
   │
   ▼
(17) BlockApiService.CreateBlockRuleAsync(request) → HTTP POST к серверу
   │
   ▼
(18) ◆ РЕШЕНИЕ: Результат запроса
    ├── [success, result != null]:
    │   ├── Saved?.Invoke(result) → BlocksViewModel получает событие
    │   ├── BlocksViewModel добавляет правило в Blockers ObservableCollection
    │   ├── _cachedRules обновляется
    │   ├── SyncRulesToLocalStoreAsync() → IBlockRuleStore.SaveRulesAsync(_cachedRules) → block_rules.json обновлён
    │   ├── IsSaving = false
    │   └── Editor = null (закрытие редактора)
    │
    ├── [result == null]:
    │   └── ErrorMessage = "Server returned empty response"
    │
    ├── [HttpRequestException]:
    │   └── ErrorMessage = ex.Message
    │
    └── [Exception]:
        └── ErrorMessage = "Error: {ex.Message}"
   │
   ▼
(19) ForegroundService на следующем тике (≤2 сек) → LoadRules() → читает обновлённый block_rules.json → новое правило активно
   │
   ▼
[End]
```

### Альтернативный путь: Отмена

```
(13a) Пользователь нажимает "Cancel" → Cancelled?.Invoke() → Editor = null → [End]
```

---

## 6. STATE MACHINE DIAGRAM — Жизненный цикл BlockEnforcementForegroundService

### Состояния

| Состояние | Описание | Инварианты |
|---|---|---|
| **[Initial]** | Сервис не создан | Нет экземпляра Service |
| **Initializing** | `OnStartCommand` вызван | Создаётся Notification Channel, строится Foreground Notification |
| **ActiveMonitoring** | Таймер тикает каждые 2 сек | `_timer != null`, `StartForeground` вызван, notification видна |
| **TickProcessing** | Внутри OnTick — обработка одного цикла | Чтение правил, проверка foreground pkg, принятие решения о блокировке |
| **Blocking** | Запущен BlockedActivity | `_lastBlockedPackage != null`, экран блокировки показан |
| **SavingUsage** | Периодическое сохранение usage | Каждые 15 минут, внутри SaveUsageLocallyIfDue |
| **Idle** | Между тиками — ожидание следующего тика таймера | Правила загружены, ожидание |
| **Destroyed** | `OnDestroy` вызван | `_timer` disposed, сервис остановлен |

### Переходы

```
[Initial] ──────────────────────────────────────────────────────────────
    │
    │ Триггер: AndroidBlockEnforcementService.Start() вызывает
    │          context.StartForegroundService(intent)
    │ Android OS создаёт экземпляр Service и вызывает OnStartCommand()
    ▼
[Initializing]
    │ Действия:
    │   1. CreateNotificationChannel() — регистрация канала "bloomdo_enforcement"
    │   2. BuildNotification() — создание persistent notification
    │   3. StartForeground(NotificationId, notification, TypeSpecialUse)
    │   4. LoadRules() — первая загрузка правил из файла
    │   5. new Timer(OnTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(2))
    │ return StartCommandResult.Sticky (системе указание перезапустить)
    ▼
[ActiveMonitoring] ←──────────────────────────────┐
    │                                              │
    │ Триггер: Timer fires (каждые 2 сек)         │
    ▼                                              │
[TickProcessing]                                   │
    │ Действия:                                    │
    │   1. LoadRules() (перечитываем правила)       │
    │   2. LoadGroupCompletion()                   │
    │   3. GetForegroundPackage()                  │
    │   4. ShouldBlock(foregroundPkg)              │
    │                                              │
    ├── [cachedRules.Count == 0]                   │
    │   → return ──────────────────────────────────┤
    │                                              │
    ├── [foregroundPkg == null]                     │
    │   → return ──────────────────────────────────┤
    │                                              │
    ├── [foregroundPkg == OwnPackage]               │
    │   → return ──────────────────────────────────┤
    │                                              │
    ├── [ShouldBlock == false]                     │
    │   → _lastBlockedPackage = null               │
    │   → goto SaveUsageCheck ─────────────────────┤
    │                                              │
    ├── [ShouldBlock == true                       │
    │    AND _lastBlockedPackage == foregroundPkg]  │
    │   → (уже блокирован, не повторяем)           │
    │   → goto SaveUsageCheck ─────────────────────┤
    │                                              │
    └── [ShouldBlock == true                       │
         AND _lastBlockedPackage != foregroundPkg] │
        ▼                                          │
[Blocking]                                         │
    │ Действия:                                    │
    │   _lastBlockedPackage = foregroundPkg         │
    │   ShowBlockedScreen() → запуск BlockedActivity│
    ▼                                              │
[SavingUsage] (SaveUsageLocallyIfDue проверка)     │
    │                                              │
    ├── [UtcNow - _lastUsageSaveUtc < 15 min]      │
    │   → пропуск ─────────────────────────────────┤
    │                                              │
    └── [≥ 15 min с последнего сохранения]          │
        Действия:                                  │
          1. UsageStatsManager.QueryAndAggregate   │
          2. Фильтрация launchable apps            │
          3. LocalUsageStore.SaveSnapshotDirect     │
          4. _lastUsageSaveUtc = UtcNow            │
        ────────────────────────────────────────────┘

[ActiveMonitoring]
    │
    │ Триггер 1: AndroidBlockEnforcementService.Stop()
    │            → context.StopService(intent)
    │            → Android вызывает OnDestroy()
    │
    │ Триггер 2: Android OS kills service (low memory)
    │            → OnDestroy() вызывается
    │            → но StartCommandResult.Sticky → OS перезапустит сервис
    │
    │ Триггер 3: Исключение в OnTick
    │            → catch в OnTick → Log.Warn → сервис НЕ падает, следующий тик продолжит
    ▼
[Destroyed]
    │ Действия:
    │   _timer?.Dispose()
    │   _timer = null
    │   base.OnDestroy()
    │
    ├── [Sticky + OS перезапуск] → [Initial] (OnStartCommand вызовется заново)
    └── [явная остановка]        → [Final]

[Final] ────── Сервис полностью остановлен
```

### Ключевые гарантии состояний

- **Sticky Service**: после уничтожения системой Android перезапустит сервис. Правила перечитаются из файла, состояние восстановится.
- **Exception safety**: OnTick обёрнут в try-catch — исключения логируются, но не убивают сервис.
- **Self-exclusion**: собственный пакет `com.CompanyName.Bloomdo.Client` никогда не блокируется (проверка на каждом тике).
- **Deduplication**: `_lastBlockedPackage` предотвращает повторный запуск BlockedActivity для того же приложения.

---

## 7. ENTITY-RELATIONSHIP DIAGRAM (ER-диаграмма)

### 7.1 Серверная база данных (PostgreSQL, EF Core)

> Все таблицы наследуют поля от `BaseEntity`: `Id (PK, Guid)`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`, `DeletedAt`, `DeletedBy`.

#### Таблица: **Accounts**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| Email | string | NOT NULL, UNIQUE |
| PasswordHash | string | NOT NULL |
| FirstName | string? | |
| LastName | string? | |
| Username | string? | UNIQUE |
| Bio | string? | |
| AvatarJson | string? | (JSON: AvatarConfig) |
| IsEmailConfirmed | bool | DEFAULT false |
| LastLoginAt | DateTime? | |
| ProfileVisibility | int (enum) | DEFAULT 0 (Public) |
| CreatedAt | DateTime | NOT NULL |
| CreatedBy | string? | |
| UpdatedAt | DateTime? | |
| UpdatedBy | string? | |
| IsDeleted | bool | DEFAULT false |
| DeletedAt | DateTime? | |
| DeletedBy | string? | |

#### Таблица: **BlockRules**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| AccountId | Guid | FK → Accounts.Id, NOT NULL |
| Title | string | NOT NULL |
| Type | int (enum) | NOT NULL (0=Schedule, 1=Limit, 2=Focus, 3=Bloomdo) |
| IsActive | bool | DEFAULT true |
| BlockedPackagesJson | string | NOT NULL, DEFAULT "[]" |
| StartTime | TimeOnly? | (для Schedule) |
| EndTime | TimeOnly? | (для Schedule) |
| ScheduleDaysJson | string? | (JSON: DayOfWeek[]) |
| DailyLimitMinutes | int? | (для Limit) |
| FocusDurationMinutes | int? | (для Focus) |
| FocusStartedAtUtc | DateTime? | (для Focus) |
| RequiredActivityGroupId | Guid? | FK → ActivityGroups.Id (для Bloomdo) |
| + BaseEntity fields | | |

**Связь**: Accounts 1 ── M BlockRules (Account владеет множеством правил)

#### Таблица: **ActivityGroups**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| AccountId | Guid | FK → Accounts.Id, NOT NULL |
| Title | string | NOT NULL |
| Icon | string | NOT NULL, DEFAULT "📋" |
| Color | string | NOT NULL, DEFAULT "#7E57C2" |
| SortOrder | int | |
| IsActive | bool | DEFAULT true |
| + BaseEntity fields | | |

**Связь**: Accounts 1 ── M ActivityGroups

#### Таблица: **ActivityItems**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| ActivityGroupId | Guid | FK → ActivityGroups.Id, NOT NULL |
| Title | string | NOT NULL |
| Description | string? | |
| TaskType | int | NOT NULL |
| DurationMinutes | int? | |
| TargetCount | int? | |
| Icon | string | NOT NULL, DEFAULT "✨" |
| Color | string | NOT NULL, DEFAULT "#7E57C2" |
| SortOrder | int | |
| IsActive | bool | DEFAULT true |
| VerificationTemplateId | int? | |
| CustomVerificationCriteria | string? | |
| + BaseEntity fields | | |

**Связь**: ActivityGroups 1 ── M ActivityItems (группа содержит задачи)

#### Таблица: **ActivityCompletions**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| ActivityItemId | Guid | FK → ActivityItems.Id, NOT NULL |
| AccountId | Guid | FK → Accounts.Id, NOT NULL |
| Date | DateOnly | NOT NULL |
| CompletedAtUtc | DateTime | NOT NULL |
| CountValue | int? | |
| Note | string? | |
| + BaseEntity fields | | |

**Связь**: ActivityItems 1 ── M ActivityCompletions  
**Связь**: Accounts 1 ── M ActivityCompletions  
**Уникальность**: (ActivityItemId, AccountId, Date) — одна задача выполняется один раз в день

#### Таблица: **DailySnapshots**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| AccountId | Guid | FK → Accounts.Id, NOT NULL |
| Date | DateOnly | NOT NULL |
| TotalScreenTimeSeconds | int | NOT NULL |
| Pickups | int | NOT NULL |
| GoalMet | bool | NOT NULL |
| + BaseEntity fields | | |

**Связь**: Accounts 1 ── M DailySnapshots  
**Уникальность**: (AccountId, Date) — один снэпшот в день

#### Таблица: **AppUsageRecords**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| AccountId | Guid | FK → Accounts.Id, NOT NULL |
| Date | DateOnly | NOT NULL |
| PackageName | string | NOT NULL |
| AppLabel | string? | |
| ForegroundSeconds | int | NOT NULL |
| + BaseEntity fields | | |

**Связь**: Accounts 1 ── M AppUsageRecords  
**Уникальность**: (AccountId, Date, PackageName) — одна запись на приложение в день

#### Таблица: **Subscriptions**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| AccountId | Guid | FK → Accounts.Id, NOT NULL |
| StripeCustomerId | string? | |
| StripeSubscriptionId | string? | |
| Plan | int (enum) | NOT NULL (SubscriptionPlan) |
| Status | int (enum) | NOT NULL (SubscriptionStatus) |
| CurrentPeriodStart | DateTime | NOT NULL |
| CurrentPeriodEnd | DateTime | NOT NULL |
| CancelAtPeriodEnd | bool | |
| CancelledAt | DateTime? | |
| + BaseEntity fields | | |

**Связь**: Accounts 1 ── 0..1 Subscriptions

#### Таблица: **Achievements**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| Code | string | NOT NULL, UNIQUE |
| Title | string | NOT NULL |
| Description | string | NOT NULL |
| Icon | string | NOT NULL |
| SortOrder | int | |
| + BaseEntity fields | | |

#### Таблица: **AccountAchievements** (связующая M:M)

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| AccountId | Guid | FK → Accounts.Id, NOT NULL |
| AchievementId | Guid | FK → Achievements.Id, NOT NULL |
| UnlockedDate | DateOnly | NOT NULL |
| + BaseEntity fields | | |

**Связь**: Accounts M ── M Achievements (через AccountAchievements)

#### Таблица: **Friendships**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| RequesterId | Guid | FK → Accounts.Id, NOT NULL |
| AddresseeId | Guid | FK → Accounts.Id, NOT NULL |
| Status | int (enum) | NOT NULL (Pending, Accepted, Declined, Blocked) |
| + BaseEntity fields | | |

**Связь**: Accounts M ── M Accounts (self-referencing через Friendships)  
**Уникальность**: (RequesterId, AddresseeId)

#### Таблица: **GroupMemberships**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| ActivityGroupId | Guid | FK → ActivityGroups.Id, NOT NULL |
| AccountId | Guid | FK → Accounts.Id, NOT NULL |
| Role | int (enum) | NOT NULL (GroupMemberRole) |
| Status | int (enum) | NOT NULL (GroupMemberStatus: Pending, Accepted) |
| + BaseEntity fields | | |

**Связь**: Accounts M ── M ActivityGroups (через GroupMemberships)

#### Таблица: **Notifications**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| RecipientId | Guid | FK → Accounts.Id, NOT NULL |
| ActorId | Guid? | FK → Accounts.Id |
| Type | int (enum) | NOT NULL (NotificationType) |
| ReferenceId | Guid? | (полиморфная ссылка на связанную сущность) |
| IsRead | bool | DEFAULT false |
| + BaseEntity fields | | |

**Связь**: Accounts 1 ── M Notifications (как получатель)

#### Таблица: **ChatConversations**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| AccountId | Guid | FK → Accounts.Id, NOT NULL |
| Title | string | NOT NULL, DEFAULT "New Chat" |
| + BaseEntity fields | | |

**Связь**: Accounts 1 ── M ChatConversations

#### Таблица: **ChatMessages**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| ConversationId | Guid | FK → ChatConversations.Id, NOT NULL |
| Role | string | NOT NULL ("user" / "assistant") |
| Content | string | NOT NULL |
| + BaseEntity fields | | |

**Связь**: ChatConversations 1 ── M ChatMessages

#### Таблица: **RefreshTokens**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| AccountId | Guid | FK → Accounts.Id, NOT NULL |
| Token | string | NOT NULL |
| ExpiresAt | DateTime | NOT NULL |
| RevokedAt | DateTime? | |
| + BaseEntity fields | | |

**Связь**: Accounts 1 ── M RefreshTokens

#### Таблица: **Roles**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| Name | string | NOT NULL, UNIQUE |
| + BaseEntity fields | | |

#### Таблица: **AccountRoles** (связующая M:M)

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| AccountId | Guid | FK → Accounts.Id |
| RoleId | Guid | FK → Roles.Id |
| + BaseEntity fields | | |

**Связь**: Accounts M ── M Roles (через AccountRoles)

#### Таблица: **RolePermissions**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| RoleId | Guid | FK → Roles.Id |
| Permission | string | NOT NULL |
| + BaseEntity fields | | |

**Связь**: Roles 1 ── M RolePermissions

#### Таблица: **StreakFreezes**

| Поле | Тип | Ограничения |
|---|---|---|
| Id | Guid | PK |
| AccountId | Guid | FK → Accounts.Id, NOT NULL |
| Date | DateOnly | NOT NULL |
| + BaseEntity fields | | |

**Связь**: Accounts 1 ── M StreakFreezes

### 7.2 Локальное хранилище клиента (файловая система JSON + SQLite)

Клиент **не использует реляционные таблицы** для основных данных. Вместо этого применяется **файловый JSON-кэш** в `AppDataDirectory`:

| Файл/Директория | Формат | Описание |
|---|---|---|
| `block_rules.json` | `List<BlockRuleResponse>` (JSON) | Кэш правил блокировки; читается ForegroundService каждые 2 сек |
| `group_completion.json` | `Dictionary<Guid, bool>` (JSON) | Статус завершения групп; читается ForegroundService |
| `usage_cache/usage_{yyyy_MM_dd}.json` | `LocalUsageSnapshot` (JSON) | Дневные снэпшоты использования; пишутся ForegroundService и UsageSyncService |
| `stats_cache/calendar_{yyyy}_{MM}.json` | `MonthCalendarResponse` (JSON) | Кэш месячного календаря (оффлайн) |
| `stats_cache/weekly_{yyyy_MM_dd}.json` | `WeeklyStatsResponse` (JSON) | Кэш недельной статистики |
| `stats_cache/daily_{yyyy_MM_dd}.json` | `DailyStatsResponse` (JSON) | Кэш дневной статистики |
| `profile_cache.json` | `LocalProfileSnapshot` (JSON) | Кэш профиля для оффлайн-режима |
| `subscription_cache.json` | `SubscriptionStatusResponse` (JSON) | Кэш статуса подписки |
| `activity_cache/daily_{yyyy_MM_dd}.json` | `DailyActivitiesResponse` (JSON) | Кэш дневных активностей |
| `timer_state.json` | `TimerStateSnapshot` (JSON) | Состояние таймера (переживает перезапуск) |
| `BloomdoLocal.db` | SQLite (EF Core) | Зарезервирована под LocalDatabaseContext (в текущей версии DbSet'ы пока не определены — контекст пуст) |

### 7.3 Сводка связей ER-диаграммы

```
Accounts ──1:M──→ BlockRules
Accounts ──1:M──→ ActivityGroups ──1:M──→ ActivityItems ──1:M──→ ActivityCompletions
Accounts ──1:M──→ ActivityCompletions
Accounts ──1:M──→ DailySnapshots
Accounts ──1:M──→ AppUsageRecords
Accounts ──1:0..1──→ Subscriptions
Accounts ──M:M──→ Achievements   (через AccountAchievements)
Accounts ──M:M──→ Accounts       (через Friendships, self-referencing)
Accounts ──M:M──→ ActivityGroups (через GroupMemberships)
Accounts ──1:M──→ Notifications
Accounts ──1:M──→ ChatConversations ──1:M──→ ChatMessages
Accounts ──1:M──→ RefreshTokens
Accounts ──M:M──→ Roles           (через AccountRoles ──1:M──→ RolePermissions)
Accounts ──1:M──→ StreakFreezes
BlockRules ──0..1:1──→ ActivityGroups  (RequiredActivityGroupId, для Bloomdo-типа)
```

---

## Приложение: Перечисления (Enums)

| Enum | Значения |
|---|---|
| `BlockType` | Schedule=0, Limit=1, Focus=2, Bloomdo=3 |
| `FriendshipStatus` | Pending, Accepted, Declined, Blocked |
| `ProfileVisibility` | Public, FollowersOnly, Private |
| `SubscriptionPlan` | Free, Monthly, Yearly |
| `SubscriptionStatus` | Active, PastDue, Cancelled, Expired |
| `NotificationType` | FollowRequest, FollowAccepted, GroupInvite, AchievementUnlocked, etc. |
| `GroupMemberRole` | Owner, Admin, Member |
| `GroupMemberStatus` | Pending, Accepted, Declined |
| `AuthorizationPolicy` | Authenticated, Premium |
| `AuthorizationFailureType` | NotAuthenticated, Forbidden |
| `ToastType` | Info, Success, Warning, Error |
