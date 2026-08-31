# Event-driven Architecture

## Vilket problem försöker arkitekturen lösa?

Event-driven Architecture försöker minska den direkta kopplingen mellan olika delar av ett program.

I stället för att en komponent måste känna till och anropa alla andra komponenter direkt publicerar den en händelse när något har hänt. Andra komponenter kan sedan prenumerera på händelsen och reagera på den självständigt.

Exempelvis kan en komponent publicera `OrderCreated`. Då kan lagerhantering minska lagersaldot, en notifiering skicka ett mejl och statistik uppdateras – utan att orderkomponenten behöver känna till detaljerna i dessa funktioner.

I vår todo-app är problemet mindre, men principen är densamma. När en todo skapas, uppdateras eller raderas behöver flera saker hända runt förändringen:

- statistik ska uppdateras,
- aktiviteten ska loggas,
- fler funktioner skulle kunna reagera i framtiden.

`TodoService` publicerar därför en händelse och behöver inte själv anropa loggning och statistik direkt.

## Vilka är de huvudsakliga komponenterna i denna arkitektur?

De eventdrivna delarna i vår lösning är:

- **Event producer** – komponenten som publicerar en händelse. Här är det `TodoService`.
- **Events** – objekt som beskriver vad som har hänt, till exempel `TodoCreated`, `TodoUpdated` och `TodoDeleted`.
- **EventBus** – tar emot publicerade händelser och skickar dem till rätt prenumeranter.
- **Event handlers/subscribers** – komponenter som reagerar på en viss händelse.
- **Sidoeffekter** – det som handlers utför, till exempel loggning och statistik.
- **Repository och datamodell** – sparar själva todo-datan. De är inte eventdrivna i sig, men producerar underlaget till händelserna.
- **API och frontend** – startar användarens operationer och visar resultatet, men de kommunicerar huvudsakligen genom HTTP i den här versionen.

## Vilket ansvar har varje komponent?

### Event producer: TodoService

`TodoService` utför själva operationen och publicerar sedan en händelse:

```csharp
repository.Add(todo);
await eventPublisher.Publish(new TodoCreated(todo));
```

Servicen behöver inte veta vilka handlers som finns. Den säger bara: ”en todo har skapats”.

### Events

Events är meddelanden om något som redan har hänt. I vår kod är de records:

```csharp
public record TodoCreated(Todo Todo) : IEvent;
public record TodoUpdated(Todo Todo) : IEvent;
public record TodoDeleted(Todo Todo) : IEvent;
```

Eventet innehåller information som subscribers kan behöva, exempelvis todo:ns ID, titel och status.

### EventBus

`EventBus` fungerar som ett nav mellan producer och subscribers. När `Publish` anropas letar den upp alla handlers som prenumererar på eventets typ och anropar dem.

Det gör att `TodoService` inte behöver ha direkta beroenden till `TodoCreatedHandler`, `FileActivityLogger` eller `StatisticsService`.

### Event handlers

Varje handler reagerar på en viss typ av händelse och har ett begränsat ansvar:

- `TodoCreatedHandler` uppdaterar skapandestatistiken och loggar den nya todo:n.
- `TodoUpdatedHandler` uppdaterar slutförandestatistiken och loggar när todo:n slutförs eller öppnas igen.
- `TodoDeletedHandler` uppdaterar raderingsstatistiken och loggar den raderade todo:n.

Om vi senare vill skicka en notis när en todo skapas kan vi lägga till en ny handler utan att ändra `TodoService`.

### Sidoeffekter

Handlers använder andra komponenter för att utföra sina sidoeffekter:

- `StatisticsService` håller statistikräknare.
- `FileActivityLogger` skriver aktivitetsmeddelanden till `log.txt`.

Det är just dessa sidoeffekter som passar bra att koppla till events. TodoService behöver fokusera på todo-regeln, medan handlers tar hand om det som ska hända runt omkring.

## Samspelar Event-driven Architecture extra bra med designmönster?

Ja. Event-driven Architecture bygger ofta på eller kombineras med flera designmönster.

### Observer pattern

EventBus-lösningen liknar Observer pattern. En producer publicerar en förändring och flera observers, här event handlers, kan reagera på den.

### Publish/subscribe

Det är också ett publish/subscribe-upplägg:

```text
TodoService publicerar TodoCreated
              ↓
          EventBus
          ↙     ↘
   Statistik   Loggning
```

Producenten publicerar utan att behöva veta exakt vilka subscribers som finns.

### Domain events

`TodoCreated`, `TodoUpdated` och `TodoDeleted` kan ses som domain events eftersom de beskriver förändringar i domänen: en todo har skapats, uppdaterats eller raderats.

### Dependency Injection

Dependency Injection används för att koppla ihop `IEventPublisher` med `EventBus` och handlers med deras beroenden. Det gör dem enklare att byta ut och testa.

### Viktig skillnad i vår implementation

Vår lösning använder event-driven principer, men det är inte en fullskalig distribuerad Event-driven Architecture. `EventBus` kör inne i samma applikation och händelserna skickas direkt i minnet.

Vi har alltså ingen queue eller message broker i projektet. `EventBus` sparar inte events för senare, utan anropar handlers direkt när `Publish` körs. Om applikationen stängs av finns det inte heller några väntande events kvar att behandla.

I en större och distribuerad lösning skulle man kunna använda exempelvis RabbitMQ, Azure Service Bus eller Kafka. Då hade events kunnat skickas mellan separata applikationer och behandlas asynkront, men det är inte den lösning vi har byggt här.

## Hur flödar data genom systemet?

### När en todo skapas

1. Användaren skriver en titel och klickar på plusknappen.
2. Frontendens `app.js` skickar en `POST` till `/todos` genom `api.js`.
3. API-endpointen i `Program.cs` tar emot requesten och anropar `TodoService.Create`.
4. `TodoService` validerar titeln och skapar ett ID.
5. Todo:n sparas i `InMemoryTodoRepository`.
6. `TodoService` publicerar `TodoCreated` genom `EventBus`.
7. `TodoCreatedHandler` tar emot eventet.
8. Handlern ökar statistiken och ber `FileActivityLogger` skriva en loggrad.
9. Eventhanteringen är klar och API:et returnerar `201 Created`.
10. Frontendens `refresh()` hämtar todos och statistik igen.
11. `app.js` renderar listan på nytt så att den nya todo:n syns på skärmen.

### När en todo markeras som klar

1. Användaren klickar på todo:ns checkbox.
2. `app.js` avgör om todo:n ska slutföras eller öppnas igen.
3. Frontend skickar `PUT /todos/{id}/complete` eller `PUT /todos/{id}/uncomplete`.
4. `TodoService` ändrar `IsCompleted` och sparar ändringen i repositoryt.
5. Servicen publicerar `TodoUpdated`.
6. `TodoUpdatedHandler` reagerar på eventet.
7. Handlern uppdaterar statistik och loggar antingen `TODO_COMPLETED` eller `TODO_REOPENED`.
8. API:et svarar frontend.
9. Frontend hämtar aktuell data på nytt och visar checkboxen som klar eller öppen.

### Vad är synkront i vår lösning?

I vår implementation väntar `TodoService` på att `EventBus.Publish` och handlers ska bli färdiga innan HTTP-svaret skickas. Händelseflödet är därför eventdrivet men fortfarande synkront i samma process.

I en mer avancerad, distribuerad lösning skulle API:et kunna lägga eventet i en meddelandekö och svara direkt. Då skulle loggning och statistik kunna uppdateras lite senare, vilket kallas eventual consistency. Det är en möjlig vidareutveckling, inte något som händer i vår nuvarande app.

## Vilka saker blir svårare med Event-driven Architecture?

Den största nackdelen är att flödet inte längre är lika enkelt att följa som ett direkt anrop från A till B. När `TodoService` publicerar ett event kan flera olika handlers reagera, och dessa reaktioner kan i sin tur skapa ytterligare händelser.

Det leder till flera utmaningar:

- **Felsökning blir svårare.** Man måste följa både producer, EventBus och alla handlers.
- **Fel kan inträffa efter att huvudoperationen lyckats.** Todo:n kan vara sparad även om loggningen misslyckas.
- **Ordning kan bli viktig.** Vissa events måste behandlas före andra.
- **Dubbletter måste hanteras.** En handler kan få samma event mer än en gång i ett köbaserat system.
- **Retries kan skapa nya problem.** Om en operation körs om måste handlern vara idempotent, alltså ge samma resultat även om den körs flera gånger.
- **Data kan vara tillfälligt ur synk.** Statistik eller notifieringar kanske uppdateras efter att huvuddata redan ändrats.
- **Eventens format behöver vara stabilt.** Om ett event ändras kan flera subscribers påverkas.
- **Testerna blir mer omfattande.** Man behöver testa både varje handler och hela eventflödet.

I vår lilla implementation blir problemen mindre eftersom allt körs lokalt och direkt, men EventBus gör fortfarande flödet mindre synligt än ett vanligt metodanrop.

## Hur hade det blivit i ett av våra större projekt?

I ett större projekt hade Event-driven Architecture kunnat vara användbar om samma händelse behöver påverka flera oberoende delar.

Exempelvis kan en händelse som `OrderCreated` leda till att:

- lager uppdateras,
- betalning kontrolleras,
- kunden får en bekräftelse,
- statistik uppdateras,
- en revisionshistorik sparas.

Utan events måste orderdelen känna till och anropa alla dessa komponenter. Med events kan varje del prenumerera på `OrderCreated` och utvecklas mer självständigt. Det hade också gjort det enklare för flera team att arbeta parallellt.

Samtidigt hade vi inte automatiskt velat använda Event-driven Architecture för varje del av ett stort projekt. För enkla och direkta operationer kan ett vanligt synkront anrop vara tydligare och enklare att underhålla.

Om vi använde denna arkitektur i ett större, distribuerat projekt hade vi eventuellt behövt komplettera den med exempelvis:

- en riktig message broker eller kö,
- persistent lagring av events,
- retries och dead-letter queues,
- idempotenta handlers,
- correlation IDs för att följa ett event genom systemet,
- strukturerad loggning och bättre övervakning,
- tydliga regler för event-versionering.

Frontendens uppdatering hade också kunnat bli mer eventdriven. I vår nuvarande app hämtar frontend data på nytt efter varje request. I ett större system skulle WebSockets eller Server-Sent Events kunna skicka förändringar direkt till användarens skärm.

## Sammanfattning

Event-driven Architecture passar bra när flera delar av ett system behöver reagera på samma händelse och när man vill minska direkt koppling mellan komponenter.

I vår todo-app publicerar `TodoService` events och låter `EventBus` skicka dem till handlers för statistik och loggning. Det gör det enkelt att lägga till nya reaktioner utan att ändra huvudlogiken.

Nackdelen är att flödet blir svårare att följa och felsöka. I större, distribuerade system behöver man dessutom hantera fördröjningar, dubbletter, retries, ordning och att data kan vara tillfälligt ur synk.

Vår implementation är därför en liten, synkron och lokal variant av event-driven architecture. Den visar grundprincipen, men saknar den meddelandekö och distribuerade infrastruktur som ofta finns i större produktionstillämpningar.
