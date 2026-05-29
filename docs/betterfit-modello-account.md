# BetterFit - Modello Account, Member e Staff

## 1. Scopo
Questo documento definisce come modellare gli account in BetterFit in modo coerente con:

- multi-tenant;
- palestre con una o piu sedi;
- utenti che possono essere clienti, staff o entrambi;
- separazione chiara tra identita, dati cliente e privilegi operativi.

L'obiettivo e evitare errori strutturali tipici, come:

- duplicare la stessa persona in tabelle diverse;
- avere account cliente e staff completamente scollegati;
- legare troppo presto i dati personali all'organizzazione sbagliata;
- gestire i permessi con flag semplici tipo `is_staff`.

---

## 2. Decisione raccomandata

BetterFit dovrebbe usare:

- un oggetto base unico: `account`;
- profili distinti per dominio:
  - `member_profile`
  - `staff_profile`
- una relazione esplicita con il tenant:
  - `gym_membership` per il cliente
  - `tenant_role_assignment` per lo staff

In pratica:

- `account` = chi e la persona;
- `member_profile` = dati cliente;
- `staff_profile` = dati operativi dello staff;
- `gym_membership` = rapporto tra persona e palestra;
- `tenant_role_assignment` = cosa puo fare quella persona nello staff.

---

## 3. Principio chiave

La stessa persona puo essere:

- solo cliente;
- solo staff;
- cliente e staff contemporaneamente.

Per questo motivo non conviene avere due mondi completamente separati, ad esempio:

- tabella `clients`
- tabella `staff_users`

Questa soluzione crea rapidamente problemi:

- duplicazione email e telefono;
- login doppi;
- problemi quando un cliente diventa coach;
- audit frammentato;
- permessi difficili da mantenere;
- migrazioni e merge complicati.

La soluzione corretta e:

- una sola identita (`account`);
- piu "profili funzionali" collegati;
- ruoli assegnati per tenant e, se serve, per sede.

---

## 4. Entita principali

## 4.1 `accounts`
Rappresenta l'identita base della persona nella piattaforma.

Campi tipici:

- `id`
- `email`
- `phone`
- `password_hash`
- `email_verified_at`
- `phone_verified_at`
- `status`
- `last_login_at`
- `created_at`
- `updated_at`

Contiene solo dati minimi di autenticazione e sicurezza.

Non dovrebbe contenere:

- certificati medici;
- abbonamenti;
- privilegi staff;
- dati fiscali specifici della palestra;
- dati di business di membership.

## 4.2 `member_profiles`
Rappresenta il profilo cliente della persona.

Campi tipici:

- `account_id`
- `first_name`
- `last_name`
- `birth_date`
- `gender` se davvero necessario
- `avatar_url`
- `emergency_contact` se previsto

Questo profilo rappresenta la persona come cliente, ma non basta ancora a dire in quale palestra sia iscritta.

## 4.3 `staff_profiles`
Rappresenta il profilo operatore/staff.

Campi tipici:

- `account_id`
- `display_name`
- `job_title`
- `internal_code`
- `active`

Questo profilo non definisce i permessi. I permessi devono stare in una relazione separata.

## 4.4 `gym_memberships`
Rappresenta il rapporto tra una persona e un tenant palestra.

Campi tipici:

- `id`
- `tenant_id`
- `account_id`
- `member_profile_id`
- `status` (`pending`, `active`, `suspended`, `cancelled`, `archived`)
- `joined_at`
- `ended_at`
- `primary_location_id`
- `source` (`self_signup`, `staff_invite`, `import`, `migration`)

Questa e una tabella fondamentale: e qui che esiste davvero il cliente della palestra.

Un account puo avere:

- zero membership;
- una membership;
- piu membership in tenant diversi;
- in casi specifici piu membership nello stesso tenant con storicizzazione.

## 4.5 `tenant_role_assignments`
Rappresenta il ruolo staff dentro il tenant.

Campi tipici:

- `id`
- `tenant_id`
- `account_id`
- `staff_profile_id`
- `role`
- `scope_type` (`tenant`, `location`)
- `scope_location_id` nullable
- `granted_at`
- `revoked_at`
- `status`

Esempi di `role`:

- `owner`
- `admin`
- `manager`
- `reception`
- `coach`
- `support_operator`

Questo modello e molto migliore di un campo semplice come `is_staff`.

---

## 5. Relazione con tenant e sedi

## 5.1 Tenant
Il tenant rappresenta la societa cliente che usa BetterFit.

## 5.2 Location
Le sedi sono figlie del tenant.

Quindi:

- `tenant` = societa
- `location` = sede fisica

Un membro puo:

- appartenere al tenant;
- avere accesso a una o piu sedi;
- avere una sede principale.

Uno staff puo:

- operare su tutto il tenant;
- operare solo su alcune sedi.

Per questo e utile prevedere anche:

- `member_location_access`
- `staff_location_assignments`

oppure incorporare la logica di scope dentro tabelle di ruolo/abilitazione.

---

## 6. Cosa NON mettere nell'oggetto base `account`

Non conviene mettere in `account` tutto il mondo applicativo.

Campi da tenere fuori:

- `medical_certificate_status`
- `subscription_plan_id`
- `membership_expiration_date`
- `is_staff`
- `is_coach`
- `fiscal_code`
- `tenant_id`

Perche:

- un account puo essere collegato a piu tenant;
- il ruolo cambia nel tempo;
- i dati cliente dipendono dal tenant;
- alcuni dati sono molto piu sensibili di altri;
- un modello troppo piatto diventa rigido subito.

---

## 7. Moduli dati collegati al membro

I dati applicativi del cliente dovrebbero essere collegati alla `gym_membership` oppure a entita dipendenti da essa.

Esempi:

- `subscriptions`
- `contracts`
- `payments`
- `invoices`
- `bookings`
- `attendance_events`
- `workout_plans`
- `measurements`
- `medical_certificates`
- `consents`

Regola pratica:

- se il dato dipende dal rapporto con una palestra, collegalo alla membership;
- se il dato descrive la persona in modo generale e trasversale, collegalo al profilo/account.

### Esempio
`nome` e `cognome` stanno bene in `member_profile`.

`abbonamento Gold valido fino al 31/12` non va in `member_profile`, ma in `subscription` o `gym_membership`.

`certificato medico caricato per la palestra X` non va in `account`, ma in `medical_certificates` collegata alla membership del tenant X.

---

## 8. Moduli dati collegati allo staff

Per lo staff il ragionamento e simile:

- il profilo operativo sta in `staff_profile`;
- i permessi stanno in `tenant_role_assignment`;
- eventuali assegnazioni a sedi o team stanno in tabelle dedicate.

Esempi:

- `coach_specializations`
- `staff_location_assignments`
- `commission_rules`
- `shift_assignments`

Anche qui:

- i dati anagrafici di login stanno nell'account;
- il fatto che una persona possa usare la dashboard di una palestra dipende dal ruolo assegnato nel tenant.

---

## 9. Casi d'uso da supportare

## 9.1 Cliente semplice

Struttura:

- `account`
- `member_profile`
- `gym_membership`

## 9.2 Staff semplice

Struttura:

- `account`
- `staff_profile`
- `tenant_role_assignment`

## 9.3 Persona sia cliente che coach

Struttura:

- `account`
- `member_profile`
- `staff_profile`
- `gym_membership`
- `tenant_role_assignment(role = coach)`

Questo e un caso comune e giustifica da solo il modello account unico + profili separati.

## 9.4 Persona cliente in piu tenant

Struttura:

- un solo `account`
- un solo `member_profile`
- piu `gym_membership`

Questo evita duplicazioni inutili, ma non implica condivisione automatica dei dati sensibili fra tenant.

---

## 10. Modello logico consigliato

```text
accounts
  -> member_profiles
  -> staff_profiles
  -> gym_memberships
       -> subscriptions
       -> contracts
       -> payments
       -> bookings
       -> attendance_events
       -> medical_certificates
       -> workout_plans
       -> consents
  -> tenant_role_assignments
       -> staff_location_assignments

gym_tenants
  -> locations
```

---

## 11. Anti-pattern da evitare

## 11.1 Due account distinti per la stessa persona
Esempio:

- `mario@email.com` come cliente
- `mario@email.com` come coach

Problemi:

- login duplicato;
- recupero password ambiguo;
- permessi incoerenti;
- UX pessima.

## 11.2 Tutto nella tabella `users`
Esempio:

- email
- password
- abbonamento
- certificato
- ruolo staff
- sede
- pagamenti

Problemi:

- schema rigido;
- colonne piene di `null`;
- forte accoppiamento;
- permessi e privacy difficili da gestire.

## 11.3 Permessi basati solo su flag
Esempio:

- `is_staff`
- `is_admin`
- `is_coach`

Problemi:

- non gestisci scope tenant/sede;
- non gestisci storicizzazione;
- non gestisci ruoli multipli;
- non gestisci revoca pulita.

---

## 12. Naming consigliato

Per evitare ambiguita, e meglio usare:

- `account` per l'identita base;
- `member_profile` per il lato cliente;
- `staff_profile` per il lato staff;
- `gym_membership` per il legame col tenant;
- `tenant_role_assignment` per i privilegi staff.

`user` come nome unico tende a diventare ambiguo molto in fretta.

---

## 13. Esempio di schema iniziale

```sql
accounts (
  id uuid pk,
  email varchar unique null,
  phone varchar unique null,
  password_hash text not null,
  status varchar not null,
  created_at timestamptz not null,
  updated_at timestamptz not null
)

member_profiles (
  id uuid pk,
  account_id uuid not null unique references accounts(id),
  first_name varchar not null,
  last_name varchar not null,
  birth_date date null,
  created_at timestamptz not null,
  updated_at timestamptz not null
)

staff_profiles (
  id uuid pk,
  account_id uuid not null unique references accounts(id),
  display_name varchar null,
  active boolean not null default true,
  created_at timestamptz not null,
  updated_at timestamptz not null
)

gym_memberships (
  id uuid pk,
  tenant_id uuid not null references gym_tenants(id),
  account_id uuid not null references accounts(id),
  member_profile_id uuid not null references member_profiles(id),
  status varchar not null,
  primary_location_id uuid null references locations(id),
  joined_at timestamptz null,
  ended_at timestamptz null,
  created_at timestamptz not null,
  updated_at timestamptz not null
)

tenant_role_assignments (
  id uuid pk,
  tenant_id uuid not null references gym_tenants(id),
  account_id uuid not null references accounts(id),
  staff_profile_id uuid not null references staff_profiles(id),
  role varchar not null,
  scope_type varchar not null,
  scope_location_id uuid null references locations(id),
  status varchar not null,
  granted_at timestamptz not null,
  revoked_at timestamptz null
)
```

Questo schema e volutamente semplice, ma gia corretto come direzione.

---

## 14. Regola pratica finale

Se ti chiedi:

> "questo dato riguarda la persona in generale oppure il suo rapporto con una palestra?"

allora:

- se riguarda la persona in generale -> `account` o `profile`
- se riguarda il rapporto con la palestra -> `membership` o entita figlia della membership
- se riguarda cosa puo fare nello staff -> `tenant_role_assignment`

---

## 15. Conclusione

La struttura raccomandata per BetterFit e:

- `account` come oggetto base unico;
- profili distinti per `member` e `staff`;
- relazioni separate per:
  - appartenenza cliente (`gym_membership`)
  - privilegi staff (`tenant_role_assignment`)

Questa impostazione:

- evita duplicati;
- gestisce bene multi-sede e multi-tenant;
- semplifica sicurezza e audit;
- rende piu pulita la privacy;
- vi lascia spazio per evolvere senza rifare il modello dopo pochi mesi.

