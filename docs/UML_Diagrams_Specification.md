# Bloomdo — Специфікація UML-діаграм

> Цей документ містить детальний опис шести UML-діаграм для системи **Bloomdo** — мобільного додатку для трекінгу щоденних активностей, блокування додатків, соціальної взаємодії та AI-чату. Кожна діаграма описана максимально детально, щоб за нею можна було побудувати повноцінну візуальну схему.

---

## 1. Діаграма Use Case (варіантів використання)

**Сценарій:** Повний огляд функціональних можливостей системи Bloomdo з точки зору акторів.

### Актори

| Актор | Опис |
|---|---|
| **Незареєстрований користувач (Guest)** | Може лише зареєструватися або увійти в систему |
| **Користувач (User)** | Основний актор. Має роль `User`, використовує всі базові функції додатку |
| **Преміум-користувач (Premium User)** | Підмножина User з активною підпискою. Має розширені ліміти (кількість блокувань, AI-повідомлень) |
| **Адмін (Admin)** | Має роль `Admin` та додаткові дозволи, визначені через `RolePermission` |
| **Зовнішні системи: Stripe** | Обробка платежів за підписки |
| **Зовнішні системи: Gemini AI** | Обробка AI-чату та верифікації фото завдань |

### Use Cases по групах

**Аутентифікація та профіль:**
- `Зареєструватися` — Guest → система (email, password, username). Створюється Account, присвоюється роль User
- `Увійти в систему` — Guest → система (email, password). Повертає JWT access + refresh token
- `Оновити токен (Refresh)` — User → система. Автоматичне оновлення access-токена за допомогою refresh-токена
- `Вийти з системи (Revoke Token)` — User → система. Інвалідація refresh-токена
- `Переглянути свій профіль` — User → система. Повертає AccountProfileResponse (username, email, bio, avatarJson, plan)
- `Редагувати профіль` — User → система. Оновлення firstName, lastName, username, bio, profileVisibility
- `Редагувати аватар` — User → система. Збереження AvatarJson (кольори шкіри, волосся, одягу, фону)

**Щоденні активності:**
- `Переглянути щоденні активності` — User → система. Завантаження DailyActivitiesResponse з групами, завданнями та статусами виконання за конкретну дату
- `Створити групу активностей` — User → система. Створення ActivityGroup (title, icon, color)
- `Редагувати групу активностей` — User → система. Зміна назви, іконки, кольору, порядку
- `Видалити групу активностей` — User → система. Soft-delete через IsDeleted
- `Створити завдання (task)` — User → система. Створення ActivityItem (title, icon, color, taskType, durationMinutes, targetCount, verificationTemplate)
- `Редагувати завдання` — User → система
- `Видалити завдання` — User → система
- `Відмітити виконання завдання (toggle)` — User → система. Створення/видалення ActivityCompletion запису за поточну дату
- `Верифікувати завдання фото` — User → система → **Gemini AI**. Надсилання фото, AI аналізує відповідність шаблону верифікації

**Блокування додатків:**
- `Переглянути правила блокування` — User → система. Список BlockRule
- `Створити правило блокування` — User → система. Тип (Schedule / Limit / Focus), blocked packages, розклад, ліміт хвилин. **«include»: Перевірити ліміт** (Free: обмежена кількість правил)
- `Редагувати правило блокування` — User → система
- `Видалити правило блокування` — User → система

**Соціальна частина:**
- `Пошук користувачів` — User → система. Пошук за username, повертає FollowStatusDto
- `Підписатися на користувача (Follow)` — User → система. Якщо профіль приватний — створюється Friendship зі статусом Pending
- `Відписатися (Unfollow)` — User → система
- `Переглянути профіль іншого користувача` — User → система. Повертає UserProfileDto (якщо дозволено ProfileVisibility)
- `Переглянути підписників / підписки` — User → система
- `Відповісти на запит підписки (Accept/Reject)` — User → система. Для приватних профілів
- `Переглянути сповіщення` — User → система. Список NotificationDto (NewFollower, GroupInvite, GroupTaskCompleted тощо)
- `Позначити сповіщення прочитаним` — User → система

**Спільні групи (Shared Groups):**
- `Переглянути свої спільні групи` — User → система. Список SharedGroupDto
- `Створити спільну групу` — User (стає Owner) → система
- `Редагувати спільну групу` — Owner → система
- `Видалити спільну групу` — Owner → система
- `Запросити учасника в групу` — Owner → система → **SignalR** сповіщення запрошеному
- `Відповісти на запрошення в групу (Accept/Reject)` — User → система
- `Видалити учасника з групи` — Owner → система
- `Переглянути деталі спільної групи` — Member → система. Учасники, завдання, прогрес кожного учасника (GroupMemberProgressDto)
- `Відмітити виконання спільного завдання` — Member → система → **SignalR** оповіщення групі

**AI-чат:**
- `Переглянути історію чатів` — User → система. Список ChatConversation
- `Надіслати повідомлення AI` — User → система → **Gemini AI**. «include»: **Перевірити ліміт повідомлень** (Free: обмеження за IFreeLimitsSettings). Контекст включає TodayLocalContext (поточні активності, прогрес)
- `Видалити чат` — User → система

**Статистика:**
- `Синхронізувати дані використання телефону` — User (Android) → система. Відправка SyncUsageRequest (список AppUsageEntry)
- `Переглянути денну статистику` — User → система. DailyStatsResponse (screen time, pickups, top apps)
- `Переглянути тижневу статистику` — User → система. WeeklyStatsResponse
- `Переглянути календар місяця` — User → система. MonthCalendarResponse (дні з GoalMet)

**Досягнення:**
- `Переглянути досягнення` — User → система. Список AchievementResponse з прапорцем IsUnlocked
- `Оцінити досягнення` — система (автоматично після виконання активностей). EvaluateAchievementsAsync

**Підписка:**
- `Переглянути статус підписки` — User → система. SubscriptionStatusResponse
- `Оформити підписку (Checkout)` — User → система → **Stripe**. Створення Checkout Session
- `Скасувати підписку` — Premium User → система → **Stripe**
- `Обробити Webhook від Stripe` — **Stripe** → система. Оновлення статусу підписки

### Зв'язки
- `Створити правило блокування` **«include»** → `Перевірити ліміт підписки`
- `Надіслати повідомлення AI` **«include»** → `Перевірити ліміт повідомлень`
- `Верифікувати завдання фото` **«extend»** → `Відмітити виконання завдання`
- `Premium User` **«extend»** → `User` (наслідує всі use cases, розширені ліміти)

---

## 2. Діаграма компонентів

**Сценарій:** Архітектурні компоненти системи Bloomdo та їх залежності. Система використовує Clean Architecture на серверній стороні та MVVM на клієнті.

### Компоненти

#### Клієнтська сторона (Android / Avalonia UI)

| Компонент | Пакет / Проект | Опис |
|---|---|---|
| **Bloomdo.Client.UI** | `Bloomdo.Client.UI` | Avalonia XAML views, converters, controls (SwipeRevealPanel). Фронтенд-шар |
| **Bloomdo.Client.Application** | `Bloomdo.Client.Application` | ViewModels (HomeViewModel, SharedGroupDetailViewModel, AiChatViewModel, BlocksViewModel, SocialViewModel, ProfileViewModel, StatsViewModel тощо), NavigationService, AuthorizationService, UsageSyncService |
| **Bloomdo.Client.Domain** | `Bloomdo.Client.Domain` | Клієнтські доменні моделі (AuthorizationResult, AppUsageInfo, TimerStateSnapshot), enums (AuthorizationPolicy, ToastType), атрибути ([Authorize]) |
| **Bloomdo.Client.Core** | `Bloomdo.Client.Core` | Інтерфейси для інфраструктурних сервісів (ISignalRClientService, INavigationService, IToastService, IAuthorizationService тощо) |
| **Bloomdo.Client.Infrastructure** | `Bloomdo.Client.Infrastructure` | HTTP API-клієнти (AuthApiService, DailyActivityApiService, SocialApiService, ChatApiService, BlockApiService, StatsApiService, ProfileApiService, SubscriptionApiService), SignalRClientService, локальні сховища (LocalActivityCache, LocalUsageStore, LocalProfileStore, LocalStatsStore, TokenStorage, AccessTokenManager), ConnectivityService, AuthHeaderHandler (DelegatingHandler для JWT) |
| **Bloomdo.Client.Android** | `Bloomdo.Client.Android` | Android-специфічний код, збір UsageStats |
| **Bloomdo.Client.Startup** | `Bloomdo.Client.Startup` | DI-реєстрація, ініціалізація додатку |

#### Серверна сторона (ASP.NET Core Web API)

| Компонент | Пакет / Проект | Опис |
|---|---|---|
| **Bloomdo.Server.Api** | `Bloomdo.Server.Api` | ASP.NET Core Web API controllers (AuthController, DailyActivitiesController, SocialController, ChatController, BlocksController, StatsController, AchievementsController, SubscriptionController), SignalR Hub (SocialHub), Middleware (ExceptionHandlingMiddleware), Authorization (PermissionAuthorizationHandler, PermissionPolicyProvider) |
| **Bloomdo.Server.Application** | `Bloomdo.Server.Application` | Бізнес-логіка: сервіси (AuthService, DailyActivityService, SocialService, ChatService, BlockService, StatsService, AchievementService, SubscriptionService, GeminiVisionService), інтерфейси репозиторіїв та сервісів |
| **Bloomdo.Server.Domain** | `Bloomdo.Server.Domain` | Domain entities (Account, ActivityGroup, ActivityItem, ActivityCompletion, Friendship, GroupMembership, ChatConversation, ChatMessage, BlockRule, Notification, Achievement, Subscription, DailySnapshot, AppUsageRecord тощо), domain exceptions |
| **Bloomdo.Server.Infrastructure** | `Bloomdo.Server.Infrastructure` | EF Core DbContext (AppDbContext), repositories (Repository, AccountRepository, ChatRepository, StatsRepository, SubscriptionRepository, RolePermissionRepository), JwtService, SocialService (ISocialService implementation), DevDataSeeder, Settings |

#### Спільний шар

| Компонент | Пакет / Проект | Опис |
|---|---|---|
| **Bloomdo.Shared** | `Bloomdo.Shared` | DTOs (Auth, Activities, Social, Chat, Blocks, Stats, Subscription, Achievements, Profile), Enums (BlockType, FriendshipStatus, GroupMemberRole, GroupMemberStatus, NotificationType, ProfileVisibility, SubscriptionPlan, SubscriptionStatus, UserRole), Constants (ApiRoutes, Permissions, AppClaimTypes) |

#### Зовнішні системи

| Компонент | Опис |
|---|---|
| **PostgreSQL** | База даних, доступ через EF Core |
| **Stripe API** | Платіжна система для підписок |
| **Google Gemini AI** | AI-чат та верифікація фото завдань |
| **SignalR** | Реальний час: сповіщення про нових підписників, запрошення в групи, виконання завдань |

### Зв'язки між компонентами (стрілки залежностей)

```
Bloomdo.Client.UI ──────────→ Bloomdo.Client.Application
Bloomdo.Client.Application ─→ Bloomdo.Client.Core
Bloomdo.Client.Application ─→ Bloomdo.Client.Domain
Bloomdo.Client.Application ─→ Bloomdo.Shared
Bloomdo.Client.Infrastructure → Bloomdo.Client.Core
Bloomdo.Client.Infrastructure → Bloomdo.Shared
Bloomdo.Client.Infrastructure ═══HTTP/SignalR═══→ Bloomdo.Server.Api
Bloomdo.Client.Startup ──────→ (all client projects)
Bloomdo.Client.Android ──────→ Bloomdo.Client.Startup

Bloomdo.Server.Api ──────────→ Bloomdo.Server.Application
Bloomdo.Server.Api ──────────→ Bloomdo.Shared
Bloomdo.Server.Application ─→ Bloomdo.Server.Domain
Bloomdo.Server.Application ─→ Bloomdo.Shared
Bloomdo.Server.Infrastructure → Bloomdo.Server.Application
Bloomdo.Server.Infrastructure → Bloomdo.Server.Domain
Bloomdo.Server.Infrastructure ═══→ PostgreSQL
Bloomdo.Server.Application ═══→ Gemini AI API
Bloomdo.Server.Application ═══→ Stripe API
```

### Інтерфейси (provided/required)

- **Bloomdo.Server.Api** виставляє REST API інтерфейс (controllers) та WebSocket інтерфейс (SocialHub)
- **Bloomdo.Client.Infrastructure** вимагає REST API та SignalR інтерфейс
- **Bloomdo.Server.Application** виставляє бізнес-інтерфейси (IAuthService, IDailyActivityService, ISocialService, IChatService тощо)
- **Bloomdo.Server.Infrastructure** реалізує інтерфейси репозиторіїв (IRepository, IAccountRepository, IChatRepository тощо)

---

## 3. Діаграма послідовності (Sequence)

**Сценарій: Відмітити виконання завдання у спільній групі з реальним часом оповіщенням інших учасників.**

Цей сценарій показує повний потік від натискання чекбокса у UI до оновлення інтерфейсу у всіх учасників групи через SignalR.

### Учасники (lifelines)

1. **:SharedGroupDetailView** — Avalonia UI (XAML view)
2. **:SharedGroupDetailViewModel** — клієнтський ViewModel
3. **:DailyActivityApiService** — HTTP-клієнт на клієнті
4. **:AuthHeaderHandler** — DelegatingHandler, додає JWT Bearer token
5. **:DailyActivitiesController** — серверний API controller
6. **:DailyActivityService** — серверний бізнес-сервіс
7. **:Repository\<ActivityCompletion\>** — репозиторій в EF Core
8. **:AppDbContext** — EF Core DbContext → PostgreSQL
9. **:ISocialRealTimeNotifier** — серверний сервіс для SignalR-оповіщень
10. **:SocialHub** — SignalR Hub на сервері
11. **:SignalRClientService** — SignalR-клієнт на мобільному додатку інших учасників
12. **:SharedGroupDetailViewModel (Member B)** — ViewModel іншого учасника

### Потік повідомлень

```
User → :SharedGroupDetailView : натискає CheckBox на завданні
:SharedGroupDetailView → :SharedGroupDetailViewModel : ToggleTaskCommand.Execute(DailyActivityItemDto)
:SharedGroupDetailViewModel → :SharedGroupDetailViewModel : Оновити IsCompleted локально (optimistic update)
:SharedGroupDetailViewModel → :DailyActivityApiService : ToggleCompletionAsync(ToggleCompletionRequest { ActivityItemId, Date })
:DailyActivityApiService → :AuthHeaderHandler : SendAsync(HttpRequestMessage)
:AuthHeaderHandler → :AuthHeaderHandler : Додати заголовок "Authorization: Bearer {JWT}"
:AuthHeaderHandler → :DailyActivitiesController : POST /api/daily/toggle
:DailyActivitiesController → :DailyActivitiesController : Витягнути AccountId з JWT claims
:DailyActivitiesController → :DailyActivityService : ToggleCompletionAsync(accountId, request)
:DailyActivityService → :Repository<ActivityCompletion> : Перевірити чи існує completion за (itemId, accountId, date)

alt [Completion не існує — потрібно створити]
    :DailyActivityService → :Repository<ActivityCompletion> : AddAsync(new ActivityCompletion { ... })
    :Repository<ActivityCompletion> → :AppDbContext : SaveChangesAsync()
else [Completion існує — потрібно видалити]
    :DailyActivityService → :Repository<ActivityCompletion> : DeleteAsync(existing)
    :Repository<ActivityCompletion> → :AppDbContext : SaveChangesAsync()
end

:DailyActivityService → :DailyActivitiesController : return true
:DailyActivitiesController → :ISocialRealTimeNotifier : NotifyTaskCompletedAsync(groupId, accountId)
:ISocialRealTimeNotifier → :SocialHub : Clients.Group("group_{groupId}").SendAsync("TaskCompleted", groupId, accountId)

--- Паралельно на пристрої Member B ---
:SocialHub → :SignalRClientService (Member B) : "TaskCompleted" event (groupId, accountId)
:SignalRClientService (Member B) → :SharedGroupDetailViewModel (Member B) : TaskCompletedReceived?.Invoke(groupId, accountId)
:SharedGroupDetailViewModel (Member B) → :SharedGroupDetailViewModel (Member B) : Оновити MemberProgresses (CompletedItems++)

--- Повертаємось до User ---
:DailyActivitiesController → :DailyActivityApiService : HTTP 200 OK (true)
:DailyActivityApiService → :SharedGroupDetailViewModel : return true
:SharedGroupDetailViewModel → :SharedGroupDetailViewModel : Якщо false — відкотити optimistic update
```

---

## 4. Діаграма активностей (Activity)

**Сценарій: Реєстрація нового користувача, проходження онбордингу та перший день використання додатку.**

### Стартова точка
● (initial node)

### Потік

```
[Start] → Відкрити додаток

→ ◇ Перевірка: чи є збережений токен?
    → [Так] → Спроба Refresh Token
        → ◇ Refresh успішний?
            → [Так] → Перехід на HomeView ──→ (перейти до "Щоденний цикл")
            → [Ні] → Перехід на LoginView
    → [Ні] → Перехід на LoginView

→ Користувач обирає "Register"
→ Ввести email, password, username
→ Натиснути "Sign Up"

→ ▐ Сервер: RegisterAsync ▐
    → Перевірити: email вже існує? → [Так] → Повернути помилку EmailAlreadyExistsException → Показати toast → Повернутися до форми
    → Перевірити: username вже існує? → [Так] → Повернути помилку UsernameAlreadyExistsException → Показати toast → Повернутися до форми
    → [OK] → Створити Account (hash password)
    → Призначити Role "User" через AccountRole
    → Згенерувати JWT access token + refresh token
    → Повернути AuthResponse

→ Клієнт зберігає токени (TokenStorage)
→ Перехід на OnboardingView

→ ═══ Онбординг (swimlane: клієнт) ═══
    → Крок 1: WelcomeStepView — привітальний екран
    → Крок 2: AskNameStepView — ввести ім'я
    → Крок 3: SetGoalsStepView — встановити цілі
    → Натиснути "Finish"

→ Перехід на MainView (HomeView як головний таб)

→ ═══ Щоденний цикл (swimlane: користувач + клієнт + сервер) ═══
    → Переглянути HomeView (щоденні активності)
    → ◇ Є групи активностей?
        → [Ні] → Натиснути "+" → Створити групу (title, icon, color)
            → POST /api/daily/groups → Зберегти ActivityGroup → Повернутися на HomeView
        → [Так] → Продовжити

    → ◇ Є завдання?
        → [Ні] → Натиснути "+" → Створити завдання (title, icon, type, duration)
            → POST /api/daily/items → Зберегти ActivityItem → Повернутися
        → [Так] → Продовжити

    → Обрати завдання для виконання
    → ◇ Завдання вимагає фото-верифікацію?
        → [Так] → Зробити фото → Надіслати на верифікацію
            → ▐ Сервер: GeminiVisionService.VerifyAsync ▐
                → Надіслати зображення Gemini AI з VerificationTemplate
                → Отримати VisionResult (Status, Confidence, Explanation)
                → ◇ Status == Verified?
                    → [Так] → Створити ActivityCompletion → Відповідь клієнту
                    → [Ні] → Повернути статус Rejected з поясненням
        → [Ні] → Натиснути чекбокс
            → POST /api/daily/toggle → Toggle ActivityCompletion

    → ◇ Всі завдання за день виконані?
        → [Так] → Оцінити досягнення (EvaluateAchievementsAsync) → Перерахувати GoalMet (RecalculateGoalMetAsync)
        → [Ні] → Продовжити виконання

→ [Кінець дня]
→ ● (final node)
```

### Swimlanes (доріжки)
1. **Користувач** — фізичні дії (натискання, введення тексту, фотографування)
2. **Клієнт (Avalonia App)** — навігація, ViewModel-логіка, локальне кешування
3. **Сервер (API)** — бізнес-логіка, валідація, збереження в БД
4. **Зовнішні системи (Gemini AI)** — верифікація фото

---

## 5. Діаграма розгортання (Deployment)

**Сценарій:** Фізичне розгортання всіх компонентів системи Bloomdo.

### Вузли (Nodes)

#### 1. «device» Android Phone
- **Середовище:** Android OS
- **Артефакти:**
  - `Bloomdo.Client.Android.apk` — Android Application Package
  - Містить: Bloomdo.Client.UI, Bloomdo.Client.Application, Bloomdo.Client.Domain, Bloomdo.Client.Core, Bloomdo.Client.Infrastructure, Bloomdo.Client.Startup
  - **Runtime:** .NET 10 (Android), Avalonia UI framework
  - **Локальне сховище:** SQLite (LocalDatabaseContext) — кешування активностей, профілю, статистики, токенів, стану таймерів
- **Комунікації:**
  - HTTPS → Application Server (REST API)
  - WSS (WebSocket Secure) → Application Server (SignalR Hub)

#### 2. «execution environment» Application Server (Linux / Docker)
- **Середовище:** ASP.NET Core 10, Kestrel
- **Артефакти:**
  - `Bloomdo.Server.Api.dll` — основний API
  - `Bloomdo.Server.Application.dll` — бізнес-логіка
  - `Bloomdo.Server.Domain.dll` — доменна модель
  - `Bloomdo.Server.Infrastructure.dll` — інфраструктура (EF Core, репозиторії)
  - `Bloomdo.Shared.dll` — спільні DTOs
- **Конфігурація:**
  - Порт 5043 (HTTP)
  - Порт 7270 (HTTPS)
  - Serilog → логування у файл `logs/log.txt`
- **SignalR Hub:** `/hubs/social` — реальний час оповіщення
- **Комунікації:**
  - TCP → Database Server (PostgreSQL, порт 5432)
  - HTTPS → Stripe API (api.stripe.com)
  - HTTPS → Google Gemini API (generativelanguage.googleapis.com)

#### 3. «database server» PostgreSQL Server
- **Середовище:** PostgreSQL 16+
- **Артефакти:**
  - База даних `bloomdo`
  - Таблиці: Accounts, ActivityGroups, ActivityItems, ActivityCompletions, Friendships, GroupMemberships, ChatConversations, ChatMessages, BlockRules, Notifications, Achievements, AccountAchievements, Subscriptions, DailySnapshots, AppUsageRecords, Roles, RolePermissions, AccountRoles, RefreshTokens, StreakFreezes
- **Міграції:** EF Core Migrations (Initial: `20260405110429_Initial`)

#### 4. «external» Stripe Payment Platform
- **Endpoint:** `https://api.stripe.com`
- **Функції:** Checkout Sessions, Subscription management, Webhooks
- **Комунікація:** Stripe → Application Server (POST webhook)

#### 5. «external» Google Gemini AI
- **Endpoint:** `https://generativelanguage.googleapis.com`
- **Функції:** AI-чат (SendMessageAsync), Photo verification (VerifyAsync)
- **Автентифікація:** API Key (до 6 ключів, round-robin)

### Зв'язки (комунікаційні шляхи)

```
[Android Phone] ──HTTPS/REST──→ [Application Server]
[Android Phone] ──WSS/SignalR──→ [Application Server]
[Application Server] ──TCP/EF Core──→ [PostgreSQL Server]
[Application Server] ──HTTPS──→ [Stripe API]
[Application Server] ──HTTPS──→ [Gemini AI API]
[Stripe API] ──HTTPS Webhook──→ [Application Server]
```

---

## 6. Діаграма класів (Class Diagram)

**Сценарій:** Доменна модель серверної частини — всі основні сутності, їх атрибути, методи та зв'язки.

### Класи

#### BaseEntity (abstract)
```
─────────────────────────
| <<abstract>>          |
| BaseEntity            |
─────────────────────────
| + Id : Guid           |
| + CreatedAt : DateTime|
| + CreatedBy : string? |
| + UpdatedAt : DateTime?|
| + UpdatedBy : string? |
| + IsDeleted : bool    |
| + DeletedAt : DateTime?|
| + DeletedBy : string? |
─────────────────────────
```

#### Account (extends BaseEntity)
```
────────────────────────────────────
| Account                          |
────────────────────────────────────
| + Email : string                 |
| + PasswordHash : string          |
| + FirstName : string?            |
| + LastName : string?             |
| + Username : string?             |
| + Bio : string?                  |
| + AvatarJson : string?           |
| + IsEmailConfirmed : bool        |
| + LastLoginAt : DateTime?        |
| + ProfileVisibility : ProfileVisibility |
────────────────────────────────────
| + AccountRoles : ICollection<AccountRole>         |
| + RefreshTokens : ICollection<RefreshToken>       |
| + InitiatedFriendships : ICollection<Friendship>  |
| + ReceivedFriendships : ICollection<Friendship>   |
| + GroupMemberships : ICollection<GroupMembership>  |
────────────────────────────────────
```

#### ActivityGroup (extends BaseEntity)
```
──────────────────────────────────────
| ActivityGroup                      |
──────────────────────────────────────
| + AccountId : Guid                 |
| + Title : string                   |
| + Icon : string                    |
| + Color : string                   |
| + SortOrder : int                  |
| + IsActive : bool                  |
──────────────────────────────────────
| + Account : Account                |
| + Items : ICollection<ActivityItem>|
| + Memberships : ICollection<GroupMembership> |
──────────────────────────────────────
```

#### ActivityItem (extends BaseEntity)
```
──────────────────────────────────────────
| ActivityItem                           |
──────────────────────────────────────────
| + ActivityGroupId : Guid               |
| + Title : string                       |
| + Description : string?               |
| + TaskType : int                       |
| + DurationMinutes : int?              |
| + TargetCount : int?                  |
| + Icon : string                        |
| + Color : string                       |
| + SortOrder : int                      |
| + IsActive : bool                      |
| + VerificationTemplateId : int?       |
| + CustomVerificationCriteria : string?|
──────────────────────────────────────────
| + Group : ActivityGroup                |
| + Completions : ICollection<ActivityCompletion> |
──────────────────────────────────────────
```

#### ActivityCompletion (extends BaseEntity)
```
────────────────────────────────
| ActivityCompletion           |
────────────────────────────────
| + ActivityItemId : Guid      |
| + AccountId : Guid           |
| + Date : DateOnly            |
| + CompletedAtUtc : DateTime  |
| + CountValue : int?          |
| + Note : string?             |
────────────────────────────────
| + ActivityItem : ActivityItem|
| + Account : Account          |
────────────────────────────────
```

#### Friendship (extends BaseEntity)
```
────────────────────────────────────
| Friendship                       |
────────────────────────────────────
| + RequesterId : Guid             |
| + AddresseeId : Guid             |
| + Status : FriendshipStatus      |
────────────────────────────────────
| + Requester : Account            |
| + Addressee : Account            |
────────────────────────────────────
```

#### GroupMembership (extends BaseEntity)
```
────────────────────────────────────
| GroupMembership                  |
────────────────────────────────────
| + ActivityGroupId : Guid         |
| + AccountId : Guid               |
| + Role : GroupMemberRole         |
| + Status : GroupMemberStatus     |
────────────────────────────────────
| + Group : ActivityGroup          |
| + Account : Account              |
────────────────────────────────────
```

#### ChatConversation (extends BaseEntity)
```
────────────────────────────────────────
| ChatConversation                     |
────────────────────────────────────────
| + AccountId : Guid                   |
| + Title : string                     |
────────────────────────────────────────
| + Account : Account                  |
| + Messages : ICollection<ChatMessage>|
────────────────────────────────────────
```

#### ChatMessage (extends BaseEntity)
```
──────────────────────────────────────
| ChatMessage                        |
──────────────────────────────────────
| + ConversationId : Guid            |
| + Role : string                    |
| + Content : string                 |
──────────────────────────────────────
| + Conversation : ChatConversation  |
──────────────────────────────────────
```

#### BlockRule (extends BaseEntity)
```
──────────────────────────────────────
| BlockRule                          |
──────────────────────────────────────
| + AccountId : Guid                 |
| + Title : string                   |
| + Type : BlockType                 |
| + IsActive : bool                  |
| + BlockedPackagesJson : string     |
| + StartTime : TimeOnly?           |
| + EndTime : TimeOnly?             |
| + ScheduleDaysJson : string?      |
| + DailyLimitMinutes : int?        |
| + FocusDurationMinutes : int?     |
──────────────────────────────────────
```

#### Notification (extends BaseEntity)
```
──────────────────────────────────────
| Notification                       |
──────────────────────────────────────
| + RecipientId : Guid               |
| + ActorId : Guid?                  |
| + Type : NotificationType          |
| + ReferenceId : Guid?              |
| + IsRead : bool                    |
──────────────────────────────────────
| + Recipient : Account              |
| + Actor : Account?                 |
──────────────────────────────────────
```

#### Achievement (extends BaseEntity)
```
────────────────────────────────────────────
| Achievement                              |
────────────────────────────────────────────
| + Code : string                          |
| + Title : string                         |
| + Description : string                   |
| + Icon : string                          |
| + SortOrder : int                        |
────────────────────────────────────────────
| + AccountAchievements : ICollection<AccountAchievement> |
────────────────────────────────────────────
```

#### AccountAchievement (extends BaseEntity)
```
────────────────────────────────────
| AccountAchievement               |
────────────────────────────────────
| + AccountId : Guid               |
| + AchievementId : Guid           |
| + UnlockedDate : DateOnly        |
────────────────────────────────────
| + Account : Account              |
| + Achievement : Achievement      |
────────────────────────────────────
```

#### Subscription (extends BaseEntity)
```
──────────────────────────────────────────
| Subscription                           |
──────────────────────────────────────────
| + AccountId : Guid                     |
| + StripeCustomerId : string?          |
| + StripeSubscriptionId : string?      |
| + Plan : SubscriptionPlan             |
| + Status : SubscriptionStatus         |
| + CurrentPeriodStart : DateTime       |
| + CurrentPeriodEnd : DateTime         |
| + CancelAtPeriodEnd : bool            |
| + CancelledAt : DateTime?            |
──────────────────────────────────────────
| + Account : Account                    |
──────────────────────────────────────────
```

#### DailySnapshot (extends BaseEntity)
```
──────────────────────────────────────
| DailySnapshot                      |
──────────────────────────────────────
| + AccountId : Guid                 |
| + Date : DateOnly                  |
| + TotalScreenTimeSeconds : int     |
| + Pickups : int                    |
| + GoalMet : bool                   |
──────────────────────────────────────
| + Account : Account                |
──────────────────────────────────────
```

#### AppUsageRecord (extends BaseEntity)
```
──────────────────────────────────────
| AppUsageRecord                     |
──────────────────────────────────────
| + AccountId : Guid                 |
| + Date : DateOnly                  |
| + PackageName : string             |
| + AppLabel : string?              |
| + ForegroundSeconds : int          |
──────────────────────────────────────
| + Account : Account                |
──────────────────────────────────────
```

#### Role
```
────────────────────────────────────
| Role                             |
────────────────────────────────────
| + Id : int                       |
| + Name : string                  |
| + Description : string?          |
| + IsActive : bool                |
| + CreatedAt : DateTime           |
────────────────────────────────────
| + AccountRoles : ICollection<AccountRole> |
────────────────────────────────────
```

#### AccountRole (extends BaseEntity)
```
────────────────────────────────────
| AccountRole                      |
────────────────────────────────────
| + AccountId : Guid               |
| + RoleId : int                   |
────────────────────────────────────
| + Account : Account              |
| + Role : Role                    |
────────────────────────────────────
```

#### RolePermission (extends BaseEntity)
```
────────────────────────────────────
| RolePermission                   |
────────────────────────────────────
| + RoleId : int                   |
| + Permission : string            |
────────────────────────────────────
| + Role : Role                    |
────────────────────────────────────
```

#### RefreshToken (extends BaseEntity)
```
────────────────────────────────────
| RefreshToken                     |
────────────────────────────────────
| + AccountId : Guid               |
| + Token : string                 |
| + ExpiresAt : DateTime           |
| + CreatedByIp : string           |
| + RevokedAt : DateTime?          |
| + RevokedByIp : string?          |
| + ReplacedByToken : string?      |
────────────────────────────────────
| + Account : Account              |
────────────────────────────────────
```

#### StreakFreeze (extends BaseEntity)
```
────────────────────────────────────
| StreakFreeze                     |
────────────────────────────────────
| + AccountId : Guid               |
| + Date : DateOnly                |
────────────────────────────────────
| + Account : Account              |
────────────────────────────────────
```

### Зв'язки (Relationships)

| Від | До | Тип | Кратність | Опис |
|---|---|---|---|---|
| Account | ActivityGroup | Composition (1 → *) | 1..* | Акаунт володіє групами активностей |
| Account | Friendship | Association (1 → *) | * (as Requester), * (as Addressee) | Двосторонній зв'язок дружби |
| Account | GroupMembership | Association (1 → *) | * | Участь у спільних групах |
| Account | AccountRole | Composition (1 → *) | 1..* | Ролі акаунта |
| Account | RefreshToken | Composition (1 → *) | * | Refresh-токени |
| Account | Subscription | Composition (1 → 0..1) | 0..1 | Підписка (опціонально) |
| Account | DailySnapshot | Composition (1 → *) | * | Щоденні знімки статистики |
| Account | AppUsageRecord | Composition (1 → *) | * | Записи використання додатків |
| Account | AccountAchievement | Association (1 → *) | * | Розблоковані досягнення |
| Account | ChatConversation | Composition (1 → *) | * | AI-чати |
| Account | Notification | Association (1 → *) | * (as Recipient) | Сповіщення |
| Account | BlockRule | Composition (1 → *) | * | Правила блокування |
| Account | StreakFreeze | Composition (1 → *) | * | Заморозки стріку |
| ActivityGroup | ActivityItem | Composition (1 → *) | * | Завдання в групі |
| ActivityGroup | GroupMembership | Association (1 → *) | * | Учасники спільної групи |
| ActivityItem | ActivityCompletion | Composition (1 → *) | * | Записи виконання |
| ChatConversation | ChatMessage | Composition (1 → *) | * | Повідомлення чату |
| Achievement | AccountAchievement | Association (1 → *) | * | Зв'язок з акаунтами |
| Role | AccountRole | Association (1 → *) | * | Акаунти з цією роллю |
| Role | RolePermission | Composition (1 → *) | * | Дозволи ролі |
| BaseEntity | Account, ActivityGroup, ActivityItem, ... | Generalization | — | Всі сутності наслідують BaseEntity (окрім Role) |

### Enums (перерахування)

| Enum | Значення |
|---|---|
| **FriendshipStatus** | Pending, Accepted, Rejected, Blocked |
| **GroupMemberRole** | Owner, Member |
| **GroupMemberStatus** | Pending, Accepted, Rejected |
| **NotificationType** | NewFollower, GroupInvite, GroupTaskCompleted, ... |
| **ProfileVisibility** | Public, Private |
| **SubscriptionPlan** | Free, Monthly, Yearly |
| **SubscriptionStatus** | Active, Canceled, PastDue, ... |
| **BlockType** | Schedule, Limit, Focus |
| **UserRole** | User, Admin |

---

## Підсумок

| # | Діаграма | Фокус |
|---|---|---|
| 1 | **Use Case** | Повний набір функцій Bloomdo з точки зору 4 акторів та 3 зовнішніх систем |
| 2 | **Компонентів** | Clean Architecture: 12 проектів, залежності, інтерфейси, зовнішні системи |
| 3 | **Послідовності** | Toggle task у спільній групі з SignalR real-time оповіщенням |
| 4 | **Активностей** | Повний user journey: реєстрація → онбординг → перший день з фото-верифікацією |
| 5 | **Розгортання** | Android Phone → ASP.NET Core Server → PostgreSQL + Stripe + Gemini AI |
| 6 | **Класів** | 17 доменних сутностей, BaseEntity ієрархія, 8 enums, всі зв'язки з кратностями |
