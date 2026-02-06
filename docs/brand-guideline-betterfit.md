# Betterfit - Brand Guideline (Colori + Layout)

## 1) Obiettivo
Definire un linguaggio visivo unico, moderno e sportivo da usare in modo uniforme su:
- dashboard web staff;
- app mobile utenti/staff;
- eventuali pagine pubbliche e comunicazioni prodotto.

Principio guida: **energia sportiva + affidabilita operativa**.

---

## 2) Personalita visiva del brand

## 2.1 Attributi del brand
- Dinamico
- Pulito
- Affidabile
- Orientato alla performance

## 2.2 Direzione estetica
- Base chiara e leggibile.
- Contrasti netti per azioni rapide.
- Accenti energetici controllati (non caotici).
- Uso limitato dei colori forti per CTA e stati critici.

---

## 3) Sistema colori (Core)

## 3.1 Palette principale
| Token | Nome | HEX | Uso principale |
|---|---|---|---|
| `bf-primary-700` | Betterfit Blue Dark | `#0A4FD4` | Hover pulsanti primari, header attivi |
| `bf-primary-600` | Betterfit Blue | `#1769FF` | CTA primarie, link, selezioni |
| `bf-primary-100` | Betterfit Blue Soft | `#E8F0FF` | sfondi informativi, chip, highlight leggeri |
| `bf-energy-500` | Energy Lime | `#B8F21D` | accenti sportivi, metriche positive, badge speciali |
| `bf-energy-100` | Energy Lime Soft | `#F2FBD2` | sfondi supporto per badge/alert positivi |
| `bf-ink-900` | Ink Dark | `#0C1424` | testi principali, titoli, sidebar dark |
| `bf-ink-700` | Ink Medium | `#334155` | testo secondario, label |
| `bf-ink-500` | Ink Light | `#64748B` | helper text |
| `bf-bg-100` | App Background | `#F4F7FC` | sfondo principale piattaforme |
| `bf-surface-100` | Surface | `#FFFFFF` | card, modali, pannelli |
| `bf-border-300` | Border | `#DCE4F2` | separatori e bordi UI |

## 3.2 Colori semantici
| Token | HEX | Uso |
|---|---|---|
| `bf-success-600` | `#16A34A` | successo, pagamento completato, accesso valido |
| `bf-warning-600` | `#D97706` | warning, rinnovo in scadenza |
| `bf-error-600` | `#DC2626` | blocchi, errori pagamento, accesso negato |
| `bf-info-600` | `#0284C7` | info neutra, hint operativi |

## 3.3 Regole di utilizzo colore
- 70% neutri (`bf-bg-*`, `bf-surface-*`, `bf-border-*`).
- 20% primario (`bf-primary-*`).
- 10% accenti (`bf-energy-*` + semantici).
- `bf-energy-500` non va usato per testo lungo o sfondi estesi.
- Un solo colore dominante per schermata: normalmente `bf-primary-600`.

## 3.4 Gradienti consigliati
- `Sprint Gradient`: `linear-gradient(135deg, #0A4FD4 0%, #1769FF 55%, #31B8FF 100%)`
- `Energy Gradient`: `linear-gradient(135deg, #B8F21D 0%, #6CEB45 100%)`

Uso gradienti:
- solo hero card, KPI speciali, onboarding, stati celebrativi;
- mai come background globale della pagina.

---

## 4) Accessibilita colore (minimo obbligatorio)
- Contrasto testo normale >= 4.5:1.
- Contrasto testo grande >= 3:1.
- Mai affidarsi solo al colore: aggiungere icona o label (`Successo`, `Errore`, `In scadenza`).
- Stati interattivi sempre differenziati: default, hover, active, disabled, focus.

Coppie sicure consigliate:
- testo `#FFFFFF` su `#1769FF`.
- testo `#0C1424` su `#F4F7FC`.
- testo `#0C1424` su `#B8F21D` solo per badge brevi.

---

## 5) Layout system uniforme

## 5.1 Griglia responsive
- Desktop: griglia 12 colonne, gutter 24 px, max width 1440 px.
- Tablet: griglia 8 colonne, gutter 16 px.
- Mobile: griglia 4 colonne, gutter 12 px.

## 5.2 Spacing scale (8-point)
- `4`, `8`, `12`, `16`, `24`, `32`, `40`, `48`, `64`.
- Padding card standard: `16` desktop, `12` mobile.
- Distanza tra sezioni pagina: `24` desktop, `16` mobile.

## 5.3 Radius, ombre, bordi
- Radius piccoli: `10px` (input/chip).
- Radius medi: `14px` (card standard).
- Radius grandi: `20px` (container principali/mobile device frame).
- Border standard: `1px solid #DCE4F2`.
- Shadow card: `0 8px 24px rgba(12, 20, 36, 0.08)`.

## 5.4 Densita UI per contesto
- Dashboard staff: densita media-alta (piu dati visibili).
- App utente: densita media-bassa (focus su task rapidi).
- Operazioni rapide staff (mobile): bottoni grandi, max 2 azioni per riga.

---

## 6) Regole layout per piattaforma

## 6.1 Dashboard web staff
Struttura consigliata:
1. Sidebar sinistra (dark, 240-280 px).
2. Topbar con ricerca, notifiche, sede.
3. Content area modulare.
4. Right rail opzionale per alert real-time.

Linee guida:
- Sidebar su fondo `bf-ink-900`, item attivo con `bf-primary-600`.
- Content su `bf-bg-100`, card `bf-surface-100`.
- KPI in prima fold (4-6 card).
- Tabella sempre con filtri persistenti e azioni riga contestuali.

## 6.2 App mobile utente
Struttura consigliata:
- Header sintetico.
- Card principali (QR accesso, prenotazione, scheda, wallet).
- Bottom navigation a 5 voci.

Linee guida:
- CTA primarie full-width o 2-up max.
- Ogni schermata con 1 obiettivo principale.
- Evitare blocchi testuali lunghi: usare summary + dettaglio espandibile.

## 6.3 Modalita staff rapida (mobile)
Struttura consigliata:
- Home "Ops" con 4-6 quick action.
- Stato dispositivi in lista ad alta leggibilita.
- Conferme obbligatorie su azioni critiche.

Linee guida:
- Pulsanti min 44 px altezza.
- Colori semaforici coerenti con dashboard.
- Feedback immediato post-azione (toast + log id).

---

## 7) Componenti UI e color mapping

## 7.1 Pulsanti
- Primary: fondo `bf-primary-600`, testo bianco.
- Primary Hover: `bf-primary-700`.
- Secondary: sfondo bianco, bordo `bf-border-300`, testo `bf-ink-900`.
- Danger: fondo `bf-error-600`, testo bianco.
- Disabled: fondo `#E2E8F0`, testo `#94A3B8`.

## 7.2 Card
- Fondo `bf-surface-100`.
- Bordo `bf-border-300`.
- Titolo `bf-ink-900`, metadati `bf-ink-500`.
- Stato card tramite barra top o badge, non con sfondo pieno invadente.

## 7.3 Tabelle
- Header: testo `bf-ink-700`, background `#F8FAFD`.
- Row hover: `#F2F7FF`.
- Row selected: `bf-primary-100`.

## 7.4 Badge e tag
- Success: background `#DCFCE7`, testo `#166534`.
- Warning: background `#FEF3C7`, testo `#92400E`.
- Error: background `#FEE2E2`, testo `#991B1B`.
- Info: background `#E0F2FE`, testo `#075985`.

## 7.5 Grafici
- Serie primaria: `bf-primary-600`.
- Serie secondaria: `#31B8FF`.
- Serie obiettivo: `bf-energy-500`.
- Serie rischio: `bf-error-600`.
- Griglia grafico: `#EAF0F8`.

---

## 8) Tipografia (supporto al layout)

Font suggerito:
- `Sora` (titoli + UI labels)
- fallback: `Avenir Next`, `Segoe UI`, sans-serif

Scala base:
- H1: 32/40 semibold
- H2: 24/32 semibold
- H3: 20/28 semibold
- Body: 14/22 regular
- Caption: 12/18 medium

Regole:
- Max 2 pesi principali per schermata.
- Mai usare testo all-caps per paragrafi lunghi.

---

## 9) Motion e feedback
- Durata animazioni: 140-220 ms.
- Easing consigliato: `ease-out`.
- Microanimazioni su hover/focus, non decorative gratuite.
- KPI e alert possono avere entrance leggera (fade + translateY 6-10px).

---

## 10) Design token pronti (CSS)
```css
:root {
  --bf-primary-700: #0A4FD4;
  --bf-primary-600: #1769FF;
  --bf-primary-100: #E8F0FF;

  --bf-energy-500: #B8F21D;
  --bf-energy-100: #F2FBD2;

  --bf-ink-900: #0C1424;
  --bf-ink-700: #334155;
  --bf-ink-500: #64748B;

  --bf-bg-100: #F4F7FC;
  --bf-surface-100: #FFFFFF;
  --bf-border-300: #DCE4F2;

  --bf-success-600: #16A34A;
  --bf-warning-600: #D97706;
  --bf-error-600: #DC2626;
  --bf-info-600: #0284C7;

  --bf-radius-sm: 10px;
  --bf-radius-md: 14px;
  --bf-radius-lg: 20px;

  --bf-shadow-card: 0 8px 24px rgba(12, 20, 36, 0.08);
}
```

---

## 11) Do / Don’t

Do:
- usare sfondi chiari e gerarchia tipografica netta;
- usare il blu Betterfit come colore guida delle azioni;
- usare il lime solo per energia/risultati/accenti;
- mantenere layout coerente tra dashboard e app.

Don’t:
- usare troppi colori saturi nella stessa schermata;
- usare gradienti come base di tutta la UI;
- usare rosso/arancio per azioni non critiche;
- variare spacing e radius in modo arbitrario tra moduli.

---

## 12) Applicazione minima obbligatoria per rilascio MVP
1. Implementare i token colore globali.
2. Uniformare bottoni, card, tabelle, badge con i token.
3. Adottare griglia 12/8/4 e spacing scale unica.
4. Allineare stati semantici (success/warning/error/info) su tutte le piattaforme.
5. Validare contrasti e accessibilita sulle schermate principali.

