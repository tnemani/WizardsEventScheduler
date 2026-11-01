## Design Write-Up

### 1. Concurrency & Capacity Enforcement
Capacity is modeled as a first-class citizen at both the database and service layers. 
* **Where it lives:** Enforced inside `EventService.RegisterPlayerAsync`, validating current registration counts against event capacity within a controlled transactional boundary.
* **Handling concurrency:** Under concurrent multi-threaded requests for the final seat, the system relies on database-level consistency checks rather than fragile UI validation. If capacity is exhausted mid-request, the operation safely rejects the transaction and returns a `409 Conflict` status code, ensuring zero overbooking.

### 2. Template Extensibility
To support multiple trading card games (Magic: The Gathering, Pokémon TCG, Yu-Gi-Oh!) without brittle `if/else` branching, the domain is decoupled into `GameTemplate` and `GameFormat` entities seeded via configuration (`appsettings.json`).
* **Open-Closed Principle:** Each template drives core event properties such as default durations and formats. 
* **Adding a 4th game:** Introducing a new trading card game—or even a non-card board game—requires **zero changes** to controllers or core service logic; it is purely handled via configuration data seeding.

### 3. Pragmatic Scope Cuts & Next Steps
To respect the 3-hour timebox while maintaining production-grade correctness, certain features were deliberately cut:
* **What was cut:** Organizer authentication, user identity management, event editing/cancellation, and automated email notifications.
* **What would be built next:** Implementing persistent distributed caching (e.g., Redis) for high-frequency event reads, adding OAuth2 authentication for store organizers, and introducing background workers for automated event status transitions.

---

## AI Collaboration Note
AI tools were leveraged as an execution accelerator to scaffold boilerplate DTOs, structure unit test boundaries, and align CSS styles with official Wizards EventLink light-mode themes. 

* **Critical Oversight Example:** Initial AI suggestions for handling concurrency relied on in-memory thread synchronization locks. Recognizing that in-memory locks fail in distributed or multi-threaded web environments interacting with persistent databases, that suggestion was rejected and replaced with strict service-layer transactional validation and robust multi-threaded SQLite test verification.