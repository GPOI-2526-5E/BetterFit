"use client";

import { useRef } from "react";
import { useGSAP } from "@gsap/react";
import gsap from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";
import {
  ArrowRight,
  CalendarRange,
  CheckCircle2,
  ChevronRight,
  CreditCard,
  Dumbbell,
  LockKeyhole,
  MessageSquareText,
  MonitorPlay,
  MoveRight,
  ScanLine,
  ShieldCheck,
  Sparkles,
  UsersRound,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

gsap.registerPlugin(useGSAP, ScrollTrigger);

const modules = [
  "Home",
  "Utenti",
  "Vendite",
  "Accessi",
  "Attivita",
  "Training",
  "CRM",
  "Analytics",
  "Impostazioni",
] as const;

const proof = [
  { label: "moduli operativi", value: "9" },
  { label: "vista staff live", value: "24h" },
  { label: "azioni tracciate", value: "audit" },
] as const;

const features = [
  {
    title: "Accessi live",
    text: "Ingressi, blocchi, varchi e override in una vista pensata per il desk.",
    icon: ScanLine,
    tone: "blue",
    span: "lg:col-span-2",
  },
  {
    title: "Profilo utente 360",
    text: "Timeline, abbonamenti, pagamenti, documenti e training nello stesso contesto.",
    icon: UsersRound,
    tone: "white",
    span: "",
  },
  {
    title: "Vendite e rinnovi",
    text: "Listino, carrello, pagamenti e ricevute senza interrompere il flusso operativo.",
    icon: CreditCard,
    tone: "white",
    span: "",
  },
  {
    title: "Training workspace",
    text: "Template, builder schede, assegnazioni e misure collegati al profilo cliente.",
    icon: Dumbbell,
    tone: "lime",
    span: "lg:col-span-2",
  },
  {
    title: "CRM e campagne",
    text: "Lead, segmenti e follow-up connessi a scadenze, rinnovi e retention.",
    icon: MessageSquareText,
    tone: "white",
    span: "",
  },
  {
    title: "Ruoli e permessi",
    text: "Accesso per sede, ruolo e responsabilita, con governance leggibile.",
    icon: ShieldCheck,
    tone: "white",
    span: "",
  },
] as const;

const dashboardShowcase = [
  {
    eyebrow: "Home operativa",
    title: "KPI, alert e task nello stesso campo visivo.",
    text: "Una home per leggere subito accessi di oggi, incassi, rinnovi e priorita dello staff.",
    tone: "blue",
    metrics: ["Accessi", "Incassi", "Task"],
  },
  {
    eyebrow: "Profilo utente 360",
    title: "Una scheda cliente che diventa piano di lavoro.",
    text: "Frame dedicato a timeline, stato abbonamento, pagamenti e quick actions contestuali.",
    tone: "lime",
    metrics: ["Timeline", "Pagamenti", "Training"],
  },
  {
    eyebrow: "Training + CRM",
    title: "Coach e commerciale leggono lo stesso dato.",
    text: "Spazio per mostrare builder schede, campagne, segmenti e avanzamento dei clienti.",
    tone: "sky",
    metrics: ["Schede", "Segmenti", "Follow-up"],
  },
] as const;

const workflow = [
  {
    step: "01",
    title: "Controllo live",
    text: "Lo staff vede accessi, alert e code operative senza aprire cinque schermate.",
    chips: ["feed accessi", "task urgenti", "stato sede"],
  },
  {
    step: "02",
    title: "Azione contestuale",
    text: "Dal cliente si passa a vendita, rinnovo, check-in manuale o comunicazione.",
    chips: ["quick actions", "audit log", "permessi"],
  },
  {
    step: "03",
    title: "Crescita misurabile",
    text: "CRM, training e analytics trasformano il lavoro quotidiano in indicatori utili.",
    chips: ["retention", "segmenti", "report"],
  },
] as const;

function ProductSurface() {
  return (
    <div className="relative mx-auto w-full max-w-[720px] py-8">
      <div className="absolute inset-x-8 top-12 h-[78%] rounded-[38%_62%_42%_58%/48%_42%_58%_52%] bg-[linear-gradient(135deg,#1769ff_0%,#31b8ff_48%,#b8f21d_100%)] opacity-95" />

      <div className="absolute left-2 top-28 z-20 hidden rounded-xl bg-[#1769ff] px-4 py-3 text-xs font-semibold text-white shadow-[0_16px_28px_rgba(23,105,255,0.24)] lg:flex">
        Prenotazioni sale e corsi
      </div>
      <div className="absolute right-4 top-16 z-20 hidden rounded-xl bg-[#b8f21d] px-4 py-3 text-xs font-semibold text-[#263a04] shadow-[0_16px_28px_rgba(184,242,29,0.24)] lg:flex">
        CRM e notifiche push
      </div>
      <div className="absolute bottom-12 left-24 z-20 hidden rounded-xl bg-[#ff6b8a] px-4 py-3 text-xs font-semibold text-white shadow-[0_16px_28px_rgba(255,107,138,0.24)] lg:flex">
        Analisi e statistiche
      </div>

      <div className="relative z-10 mx-auto w-[88%]">
        <div className="rounded-[26px] bg-[#0a1022] p-3 shadow-[0_28px_60px_rgba(12,20,36,0.28)]">
          <div className="overflow-hidden rounded-[18px] bg-[#f4f7fc]">
            <div className="grid min-h-[340px] lg:grid-cols-[170px_minmax(0,1fr)]">
              <aside className="bg-[#0c1424] p-4 text-[#dde7f6]">
                <div className="mb-5 flex items-center gap-3">
                  <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-white/10">
                    <LockKeyhole className="h-4 w-4 text-[#b8f21d]" />
                  </div>
                  <div>
                    <p className="text-sm font-semibold">BetterFit</p>
                    <p className="text-xs text-slate-400">FitUp Roma Nord</p>
                  </div>
                </div>

                <nav className="space-y-1.5">
                  {modules.slice(0, 8).map((module, index) => (
                    <div
                      key={module}
                      className={`flex items-center justify-between rounded-xl px-3 py-2 text-sm ${
                        index === 0 ? "bg-[#1769ff] text-white" : "text-slate-300"
                      }`}
                    >
                      <span>{module}</span>
                      <span className={`h-1.5 w-1.5 rounded-full ${index === 0 ? "bg-[#b8f21d]" : "bg-white/18"}`} />
                    </div>
                  ))}
                </nav>
              </aside>

              <div className="p-4">
                <div className="mb-4 flex items-center justify-between">
                  <div>
                    <p className="text-xs font-semibold uppercase text-slate-500">Panoramica operativa</p>
                    <p className="mt-1 text-lg font-semibold text-[#0c1424]">Sede sotto controllo</p>
                  </div>
                  <div className="rounded-full bg-[#e8f0ff] px-3 py-1 text-xs font-semibold text-[#0a4fd4]">
                    Oggi
                  </div>
                </div>

                <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_150px]">
                  <div className="rounded-[16px] bg-[linear-gradient(135deg,#0a4fd4_0%,#1769ff_60%,#31b8ff_100%)] p-4 text-white">
                    <div className="grid grid-cols-3 gap-2">
                      {[
                        ["Accessi", "182"],
                        ["Rinnovi", "16"],
                        ["Incasso", "4.2k"],
                      ].map(([label, value]) => (
                        <div key={label} className="rounded-2xl bg-white/12 px-3 py-3">
                          <p className="text-xs text-white/68">{label}</p>
                          <p className="mt-2 text-2xl font-semibold">{value}</p>
                        </div>
                      ))}
                    </div>
                    <div className="mt-5 h-28 rounded-2xl bg-white/10 p-4">
                      <div className="flex h-full items-end gap-2">
                        {[44, 70, 52, 88, 64, 92, 76, 96].map((height, index) => (
                          <span
                            key={index}
                            className="flex-1 rounded-t-lg bg-white/45"
                            style={{ height: `${height}%` }}
                          />
                        ))}
                      </div>
                    </div>
                  </div>

                  <div className="rounded-[16px] border border-[#dce4f2] bg-white p-4">
                    <p className="text-xs font-semibold uppercase text-[#0a4fd4]">Task</p>
                    <div className="mt-4 space-y-3">
                      {["Certificati", "Pagamenti", "Lead"].map((item, index) => (
                        <div key={item} className="flex items-center justify-between rounded-xl bg-[#f8fbff] px-3 py-2">
                          <span className="text-sm text-slate-700">{item}</span>
                          <span className="rounded-full bg-[#e8f0ff] px-2 py-1 text-xs font-semibold text-[#0a4fd4]">
                            {index + 2}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>

                <div className="mt-3 grid grid-cols-3 gap-3">
                  {[
                    ["Profilo 360", "Azioni e timeline"],
                    ["Training", "48 schede"],
                    ["CRM", "6 campagne"],
                  ].map(([label, value]) => (
                    <div key={label} className="rounded-[16px] border border-[#dce4f2] bg-white p-3">
                      <p className="text-xs font-semibold uppercase text-slate-500">{label}</p>
                      <p className="mt-2 text-sm font-semibold text-[#0c1424]">{value}</p>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className="mx-auto h-4 w-[82%] rounded-b-[26px] bg-[#111827] shadow-[0_18px_30px_rgba(12,20,36,0.28)]" />
        <div className="mx-auto h-2 w-[46%] rounded-b-full bg-[#8fa2bd]" />
      </div>

      <div className="absolute bottom-4 right-10 z-20 hidden w-32 rounded-[22px] bg-[#0a1022] p-2 shadow-[0_24px_40px_rgba(12,20,36,0.25)] lg:block">
        <div className="rounded-[16px] bg-white p-3">
          <div className="mb-4 h-5 w-20 rounded-full bg-[#1769ff]" />
          <div className="space-y-2">
            {["Check-in", "Corsi", "Training", "Chat"].map((item) => (
              <div key={item} className="rounded-xl bg-[#f4f7fc] px-3 py-2 text-xs font-semibold text-slate-600">
                {item}
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function DashboardFrame({
  tone,
  metrics,
}: {
  tone: (typeof dashboardShowcase)[number]["tone"];
  metrics: readonly string[];
}) {
  const toneClass =
    tone === "lime"
      ? "bg-[linear-gradient(180deg,#fbfee9_0%,#ffffff_100%)] border-[#dfeeb0]"
      : tone === "sky"
        ? "bg-[linear-gradient(180deg,#f3fbff_0%,#ffffff_100%)] border-[#c8e7f8]"
        : "bg-[linear-gradient(180deg,#ffffff_0%,#edf4ff_100%)] border-[#bfd2f7]";

  return (
    <div className="overflow-hidden rounded-[22px] border border-[#dce4f2] bg-[#f7faff] p-3 shadow-[0_16px_32px_rgba(12,20,36,0.08)]">
      <div className="mb-3 flex items-center justify-between rounded-2xl border border-white/80 bg-white/90 px-4 py-3">
        <div className="flex items-center gap-2">
          <span className="h-2.5 w-2.5 rounded-full bg-[#1769ff]" />
          <span className="h-2.5 w-2.5 rounded-full bg-[#b8f21d]" />
          <span className="h-2.5 w-2.5 rounded-full bg-[#d97706]" />
        </div>
        <p className="text-xs font-semibold uppercase text-slate-400">BetterFit dashboard</p>
      </div>

      <div className={`relative aspect-[16/10] overflow-hidden rounded-[18px] border border-dashed ${toneClass}`}>
        <div className="absolute inset-0 bf-grid opacity-50" />
        <div className="absolute left-5 right-5 top-5 flex items-center justify-between">
          <div className="h-3 w-32 rounded-full bg-[#1769ff]/18" />
          <div className="h-3 w-16 rounded-full bg-[#b8f21d]/45" />
        </div>
        <div className="absolute left-5 top-14 h-24 w-[42%] rounded-2xl bg-white/82 shadow-[0_10px_22px_rgba(12,20,36,0.06)]" />
        <div className="absolute right-5 top-14 h-24 w-[48%] rounded-2xl bg-white/72 shadow-[0_10px_22px_rgba(12,20,36,0.06)]" />
        <div className="absolute bottom-5 left-5 right-5 grid gap-3 sm:grid-cols-3">
          {metrics.map((metric) => (
            <div
              key={metric}
              className="rounded-2xl border border-white/80 bg-white/82 px-3 py-3 text-center text-xs font-semibold uppercase text-slate-500"
            >
              {metric}
            </div>
          ))}
        </div>
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="flex h-16 w-16 items-center justify-center rounded-[18px] bg-white text-[#1769ff] shadow-[0_16px_24px_rgba(23,105,255,0.12)]">
            <MonitorPlay className="h-7 w-7" />
          </div>
        </div>
      </div>
    </div>
  );
}

export function LandingPage() {
  const scope = useRef<HTMLDivElement>(null);

  useGSAP(
    () => {
      if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
        return;
      }

      const hero = gsap.timeline({ defaults: { ease: "power3.out" } });
      hero
        .from("[data-nav]", { autoAlpha: 0, y: -18, duration: 0.7 })
        .from("[data-hero-line]", { autoAlpha: 0, y: 26, duration: 0.75, stagger: 0.1 }, "-=0.35")
        .from("[data-surface]", { autoAlpha: 0, y: 38, scale: 0.97, duration: 0.95 }, "-=0.55");

      gsap.utils.toArray<HTMLElement>("[data-reveal]").forEach((node) => {
        gsap.from(node, {
          autoAlpha: 0,
          y: 42,
          duration: 0.82,
          ease: "power3.out",
          scrollTrigger: {
            trigger: node,
            start: "top 84%",
            once: true,
          },
        });
      });

      gsap.utils.toArray<HTMLElement>("[data-bento]").forEach((node, index) => {
        gsap.from(node, {
          autoAlpha: 0,
          y: 42,
          scale: 0.98,
          duration: 0.72,
          delay: index * 0.035,
          ease: "power3.out",
          scrollTrigger: {
            trigger: node,
            start: "top 88%",
            once: true,
          },
        });
      });

      gsap.to("[data-parallax='surface']", {
        yPercent: -7,
        ease: "none",
        scrollTrigger: {
          trigger: scope.current,
          start: "top top",
          end: "bottom top",
          scrub: true,
        },
      });

      gsap.fromTo(
        "[data-progress]",
        { scaleY: 0 },
        {
          scaleY: 1,
          transformOrigin: "top center",
          ease: "none",
          scrollTrigger: {
            trigger: "#workflow",
            start: "top 70%",
            end: "bottom bottom",
            scrub: true,
          },
        }
      );
    },
    { scope }
  );

  return (
    <main id="top" ref={scope} className="relative overflow-hidden">
      <section className="relative overflow-hidden border-b border-[#dce4f2]/70 bg-white">
        <div className="absolute inset-0 -z-10 bg-[linear-gradient(115deg,#ffffff_0%,#f8fbff_52%,#f2fbd2_100%)]" />

        <div className="mx-auto flex min-h-[100svh] max-w-7xl flex-col px-6 pb-10 lg:px-8">
          <header
            data-nav
            className="-mx-6 flex items-center justify-between bg-[#030738] px-6 py-4 text-white shadow-[0_18px_36px_rgba(3,7,56,0.2)] lg:-mx-8 lg:px-8"
          >
            <div className="flex items-center gap-3">
              <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-[linear-gradient(135deg,#1769ff_0%,#31b8ff_100%)] text-white shadow-[0_12px_24px_rgba(23,105,255,0.22)]">
                <MonitorPlay className="h-4 w-4" />
              </div>
              <div>
                <p className="text-sm font-semibold uppercase text-white">BetterFit</p>
                <p className="text-xs text-white/58">Gestionale palestre</p>
              </div>
            </div>

            <nav className="hidden items-center gap-8 lg:flex">
              <a href="#funzionalita" className="text-xs font-semibold uppercase text-white/76 transition hover:text-white">
                Funzionalita
              </a>
              <a href="#dashboard" className="text-xs font-semibold uppercase text-white/76 transition hover:text-white">
                Dashboard
              </a>
              <a href="#workflow" className="text-xs font-semibold uppercase text-white/76 transition hover:text-white">
                Workflow
              </a>
              <a href="#cta" className="rounded-full bg-[#b8f21d] px-5 py-3 text-xs font-semibold uppercase text-[#152005] transition hover:bg-white">
                Demo
              </a>
            </nav>
          </header>

          <div className="grid flex-1 items-center gap-10 py-10 lg:grid-cols-[minmax(0,0.82fr)_minmax(0,1.18fr)]">
            <div className="max-w-2xl">
              <p data-hero-line className="mb-5 text-sm font-semibold uppercase text-slate-400">
                Software gestionale BetterFit
              </p>
              <h1
                data-hero-line
                className="max-w-xl text-5xl font-semibold leading-[0.98] text-[#1769ff] sm:text-6xl lg:text-7xl"
              >
                Il gestionale palestre e club sportivi completo
              </h1>
              <p
                data-hero-line
                className="mt-6 max-w-lg text-lg font-semibold leading-8 text-[#0c1424]"
              >
                Accessi, vendite, profilo utente, training, CRM e analytics in una sola
                piattaforma per far crescere il tuo centro senza rallentare il desk.
              </p>
              <p data-hero-line className="mt-5 max-w-lg text-base leading-7 text-slate-600">
                Pensato per palestre, box, piscine e club sportivi che vogliono una gestione
                chiara, veloce e davvero usabile dallo staff ogni giorno.
              </p>

              <div data-hero-line className="mt-9 flex flex-col gap-3 sm:flex-row">
                <Button asChild size="lg">
                  <a href="#cta">
                    Prenota una demo
                    <ArrowRight className="h-4 w-4" />
                  </a>
                </Button>
                <Button asChild variant="outline" size="lg">
                  <a href="#dashboard">
                    Guarda la dashboard
                    <ChevronRight className="h-4 w-4" />
                  </a>
                </Button>
              </div>

              <div data-hero-line className="mt-10 grid max-w-lg grid-cols-3 divide-x divide-[#dce4f2] border-y border-[#dce4f2] py-4">
                {proof.map((item) => (
                  <div key={item.label} className="px-4 first:pl-0">
                    <p className="text-xl font-semibold text-[#0c1424]">{item.value}</p>
                    <p className="mt-1 text-xs leading-5 text-slate-500">{item.label}</p>
                  </div>
                ))}
              </div>
            </div>

            <div data-surface data-parallax="surface" className="relative">
              <ProductSurface />
            </div>
          </div>
        </div>
      </section>

      <section id="funzionalita" className="px-6 py-20 lg:px-8">
        <div className="mx-auto max-w-7xl">
          <div data-reveal className="grid gap-8 lg:grid-cols-[minmax(0,0.72fr)_minmax(0,1fr)] lg:items-end">
            <div>
              <p className="text-sm font-semibold uppercase text-[#0a4fd4]">Funzionalita</p>
              <h2 className="mt-4 max-w-xl text-4xl font-semibold leading-tight text-[#0c1424] sm:text-5xl">
                Moduli pochi, forti, collegati fra loro.
              </h2>
            </div>
            <p className="max-w-xl text-lg leading-8 text-slate-600">
              Ogni area e pensata per un flusso reale: desk, commerciale, coach e manager
              leggono dati diversi senza uscire dallo stesso sistema.
            </p>
          </div>

          <div className="mt-12 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {features.map(({ title, text, icon: Icon, tone, span }) => (
              <article
                key={title}
                data-bento
                className={`group relative min-h-64 overflow-hidden rounded-[22px] border p-6 shadow-[0_12px_28px_rgba(12,20,36,0.07)] transition duration-300 hover:-translate-y-1 ${span} ${
                  tone === "blue"
                    ? "border-[#1769ff]/20 bg-[linear-gradient(135deg,#0a4fd4_0%,#1769ff_62%,#31b8ff_100%)] text-white"
                    : tone === "lime"
                      ? "border-[#dfeeb0] bg-[linear-gradient(135deg,#f8fde8_0%,#ffffff_68%)] text-[#0c1424]"
                      : "border-white/80 bg-white/86 text-[#0c1424]"
                }`}
              >
                <div className="relative flex h-full flex-col">
                  <div className="flex items-center justify-between">
                    <div
                      className={`flex h-11 w-11 items-center justify-center rounded-2xl ${
                        tone === "blue" ? "bg-white/14 text-[#b8f21d]" : "bg-[#e8f0ff] text-[#1769ff]"
                      }`}
                    >
                      <Icon className="h-5 w-5" />
                    </div>
                    <MoveRight className={`h-4 w-4 transition group-hover:translate-x-1 ${tone === "blue" ? "text-white/72" : "text-[#1769ff]"}`} />
                  </div>
                  <div className="mt-auto pt-12">
                    <h3 className="text-2xl font-semibold leading-tight">{title}</h3>
                    <p className={`mt-3 max-w-md text-base leading-7 ${tone === "blue" ? "text-white/76" : "text-slate-600"}`}>
                      {text}
                    </p>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section id="dashboard" className="px-6 py-20 lg:px-8">
        <div className="mx-auto max-w-7xl">
          <div data-reveal className="max-w-3xl">
            <Badge className="mb-6">
              <MonitorPlay className="h-3.5 w-3.5" />
              Dashboard
            </Badge>
            <h2 className="max-w-2xl text-4xl font-semibold leading-tight text-[#0c1424] sm:text-5xl">
              Le viste principali del lavoro quotidiano.
            </h2>
            <p className="mt-5 max-w-xl text-lg leading-8 text-slate-600">
              Home, profilo cliente e training raccontano il prodotto dove conta di piu:
              operativita, controllo e crescita del centro.
            </p>
          </div>

          <div className="mt-12 space-y-8">
            {dashboardShowcase.map((item, index) => {
              const reverse = index % 2 === 1;

              return (
                <article
                  key={item.title}
                  data-reveal
                  className="grid items-center gap-8 border-t border-[#dce4f2] pt-8 lg:grid-cols-[minmax(0,0.82fr)_minmax(0,1.18fr)]"
                >
                  <div className={reverse ? "lg:order-2" : ""}>
                    <p className="text-sm font-semibold uppercase text-[#0a4fd4]">{item.eyebrow}</p>
                    <h3 className="mt-3 max-w-lg text-3xl font-semibold leading-tight text-[#0c1424]">
                      {item.title}
                    </h3>
                    <p className="mt-4 max-w-lg text-base leading-7 text-slate-600">{item.text}</p>
                    <div className="mt-6 flex flex-wrap gap-2">
                      <Badge variant="outline">{item.tone}</Badge>
                      <Badge variant="outline">vista {index + 1}</Badge>
                    </div>
                  </div>

                  <div className={reverse ? "lg:order-1" : ""}>
                    <DashboardFrame tone={item.tone} metrics={item.metrics} />
                  </div>
                </article>
              );
            })}
          </div>
        </div>
      </section>

      <section id="workflow" className="px-6 py-20 lg:px-8">
        <div className="mx-auto grid max-w-7xl gap-10 lg:grid-cols-[minmax(0,0.78fr)_minmax(0,1.22fr)]">
          <div data-reveal className="lg:sticky lg:top-12 lg:self-start">
            <Badge className="mb-6">
              <CheckCircle2 className="h-3.5 w-3.5" />
              Workflow
            </Badge>
            <h2 className="max-w-md text-4xl font-semibold leading-tight text-[#0c1424] sm:text-5xl">
              Dal check-in al rinnovo, senza cambiare ritmo.
            </h2>
            <p className="mt-5 max-w-md text-lg leading-8 text-slate-600">
              Ogni passaggio e pensato per ridurre attrito al desk e mantenere il dato utile anche
              per manager, coach e commerciale.
            </p>
          </div>

          <div className="relative pl-8 sm:pl-10">
            <div className="absolute left-0 top-0 h-full w-px bg-[#dce4f2]" />
            <div
              data-progress
              className="absolute left-0 top-0 h-full w-px origin-top bg-[linear-gradient(180deg,#1769ff_0%,#b8f21d_100%)]"
            />

            <div className="space-y-5">
              {workflow.map((item) => (
                <article
                  key={item.step}
                  data-reveal
                  className="relative rounded-[22px] border border-white/80 bg-white/84 p-6 shadow-[0_12px_28px_rgba(12,20,36,0.07)] backdrop-blur-md"
                >
                  <div className="absolute left-[-2.65rem] top-8 flex h-8 w-8 items-center justify-center rounded-full border border-[#dce4f2] bg-white text-xs font-semibold text-[#0a4fd4]">
                    {item.step}
                  </div>
                  <p className="text-sm font-semibold uppercase text-[#0a4fd4]">Step {item.step}</p>
                  <h3 className="mt-3 text-2xl font-semibold leading-tight text-[#0c1424]">{item.title}</h3>
                  <p className="mt-3 max-w-xl text-base leading-7 text-slate-600">{item.text}</p>
                  <div className="mt-6 flex flex-wrap gap-2">
                    {item.chips.map((chip) => (
                      <span
                        key={chip}
                        className="rounded-full border border-[#dce4f2] bg-[#f8fbff] px-3 py-1.5 text-xs font-semibold uppercase text-slate-500"
                      >
                        {chip}
                      </span>
                    ))}
                  </div>
                </article>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section id="cta" className="px-6 pb-24 pt-8 lg:px-8">
        <div
          data-reveal
          className="mx-auto max-w-7xl overflow-hidden rounded-[28px] bg-[#0c1424] px-6 py-10 text-white shadow-[0_28px_60px_rgba(12,20,36,0.22)] sm:px-10 lg:px-14 lg:py-14"
        >
          <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_minmax(0,0.62fr)] lg:items-end">
            <div>
              <Badge variant="energy" className="mb-6 border-white/0">
                <Sparkles className="h-3.5 w-3.5" />
                Demo BetterFit
              </Badge>
              <h2 className="max-w-2xl text-4xl font-semibold leading-tight sm:text-5xl">
                Porta il tuo centro in una gestione piu chiara, veloce e misurabile.
              </h2>
              <p className="mt-5 max-w-2xl text-lg leading-8 text-slate-300">
                Dal primo check-in del giorno al report direzionale, BetterFit tiene insieme
                persone, pagamenti, accessi e allenamento.
              </p>
            </div>

            <div className="space-y-3 rounded-[22px] border border-white/10 bg-white/6 p-5">
              <div className="flex items-center gap-3 rounded-2xl bg-white/6 px-4 py-3">
                <MonitorPlay className="h-5 w-5 text-[#31b8ff]" />
                <p className="text-sm text-slate-200">Dashboard staff per ogni turno</p>
              </div>
              <div className="flex items-center gap-3 rounded-2xl bg-white/6 px-4 py-3">
                <CalendarRange className="h-5 w-5 text-[#b8f21d]" />
                <p className="text-sm text-slate-200">App, CRM e training nello stesso gestionale</p>
              </div>
              <Button asChild size="lg" className="w-full">
                <a href="#top">
                  Torna all&apos;inizio
                  <ArrowRight className="h-4 w-4" />
                </a>
              </Button>
            </div>
          </div>
        </div>
      </section>
    </main>
  );
}
