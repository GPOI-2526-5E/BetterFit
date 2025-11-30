<script>
	import { m } from "$lib/paraglide/messages";
	import AnimatedBusinessType from '$lib/components/AnimatedBusinessType.svelte';
	import { AspectRatio } from "$lib/components/ui/aspect-ratio/index.js";
	import Button from "$lib/components/ui/button/button.svelte";
	import * as Card from "$lib/components/ui/card/index.js";
	import { Input } from "$lib/components/ui/input/index.js";
	import { Label } from "$lib/components/ui/label/index.js";
	import * as Select from "$lib/components/ui/select/index.js";

	let selectedBusinessType = $state({ value: "", label: "Select your business type" });
</script>
<div class="min-h-screen w-full">
	<!-- Hero Section -->
	<section class="w-full px-4 md:px-6 py-20 md:py-32 bg-gradient-to-b from-background to-muted/20">
		<div class="max-w-7xl mx-auto">
			<div class="flex flex-col lg:flex-row items-center gap-16">
				<div class="flex-1 space-y-8 max-w-2xl">
					<div class="inline-block px-4 py-2 bg-primary/10 rounded-full text-sm font-medium text-primary">
                        {m.hero_badge()}
					</div>
					<h1 class="text-5xl md:text-7xl font-black tracking-tight leading-tight">
						{m.landing_heading_1()}<br/><AnimatedBusinessType />
					</h1>
					<p class="text-xl md:text-2xl text-muted-foreground leading-relaxed">
						{m.hero_description()}
					</p>
					<div class="flex flex-col sm:flex-row gap-4 pt-4">

                        <Button size="lg" href="#waitlist">
                            {m.cta_waitlist()} 	<svg class="ml-2 w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6"/>
							</svg>
                        </Button>
						
					</div>
                    <!--
					<div class="flex items-center gap-8 pt-4 text-sm text-muted-foreground">
						<div class="flex items-center gap-2">
							<svg class="w-5 h-5 text-green-600" fill="currentColor" viewBox="0 0 20 20">
								<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
							</svg>
							No credit card required
						</div>
						<div class="flex items-center gap-2">
							<svg class="w-5 h-5 text-green-600" fill="currentColor" viewBox="0 0 20 20">
								<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
							</svg>
							14-day free trial
						</div>
					</div>-->
				</div>
				
				<div class="flex-1 flex justify-center lg:justify-end">
					<svg class="w-full max-w-[600px]" viewBox="0 0 600 400" xmlns="http://www.w3.org/2000/svg">
						<defs>
							<filter id="shadow" x="-50%" y="-50%" width="200%" height="200%">
								<feGaussianBlur in="SourceAlpha" stdDeviation="8"/>
								<feOffset dx="0" dy="8" result="offsetblur"/>
								<feFlood flood-color="#000000" flood-opacity="0.3"/>
								<feComposite in2="offsetblur" operator="in"/>
								<feMerge>
									<feMergeNode/>
									<feMergeNode in="SourceGraphic"/>
								</feMerge>
							</filter>
						</defs>

						<!-- Abstract Blob -->
						<path d="M480 300C450 350 350 380 250 360C150 340 50 250 80 150C110 50 250 20 350 40C450 60 510 250 480 300Z" fill="#00cc66" opacity="0.1"/>

						<!-- Main Dashboard Card -->
						<g filter="url(#shadow)">
							<rect x="150" y="80" width="300" height="220" rx="12" fill="#f7fcf9"/>
							<path d="M150 92C150 85.3726 155.373 80 162 80H210V300H162C155.373 300 150 294.627 150 288V92Z" fill="#eafdf2"/>
							<rect x="165" y="110" width="20" height="4" rx="2" fill="#88e0b0"/>
							<rect x="165" y="125" width="20" height="4" rx="2" fill="#88e0b0"/>
							<rect x="165" y="140" width="20" height="4" rx="2" fill="#88e0b0"/>
							<rect x="235" y="110" width="100" height="8" rx="4" fill="#aeb3bc"/>
							<rect x="235" y="130" width="60" height="4" rx="2" fill="#d1d5db"/>
							<rect x="235" y="240" width="20" height="40" rx="2" fill="#3d8f65"/>
							<rect x="265" y="200" width="20" height="80" rx="2" fill="#66d691"/>
							<rect x="295" y="160" width="20" height="120" rx="2" fill="#00cc66"/>
							<rect x="325" y="190" width="20" height="90" rx="2" fill="#99e65c"/>
							<rect x="355" y="220" width="20" height="60" rx="2" fill="#3d8f65"/>
						</g>

						<!-- Notification Card -->
						<g transform="translate(80, 220)" filter="url(#shadow)">
							<rect x="0" y="0" width="140" height="60" rx="8" fill="#f7fcf9"/>
							<circle cx="30" cy="30" r="15" fill="#eafdf2" stroke="#00cc66" stroke-width="2"/>
							<rect x="55" y="20" width="60" height="6" rx="3" fill="#aeb3bc"/>
							<rect x="55" y="34" width="40" height="4" rx="2" fill="#00cc66"/>
						</g>

						<!-- Gym Dumbbell -->
						<g transform="translate(430, 250) rotate(-10)" filter="url(#shadow)">
							<rect x="0" y="12" width="80" height="12" rx="4" fill="#aeb3bc"/>
							<rect x="-10" y="0" width="25" height="36" rx="6" fill="#ff4444"/>
							<rect x="65" y="0" width="25" height="36" rx="6" fill="#ff4444"/>
							<path d="M-5 5C-5 5 0 5 0 15" stroke="#FFFFFF" stroke-width="2" stroke-linecap="round" opacity="0.5"/>
							<path d="M70 5C70 5 75 5 75 15" stroke="#FFFFFF" stroke-width="2" stroke-linecap="round" opacity="0.5"/>
						</g>
					</svg>
				</div>
			</div>
		</div>
	</section>

	<!-- Dashboard Preview Section -->
	<section class="w-full px-4 md:px-6 py-24">
		<div class="max-w-7xl mx-auto">
			<div class="text-center space-y-4 mb-12">
				<div class="inline-block px-4 py-2 bg-primary/10 rounded-full text-sm font-medium text-primary">
					{m.dashboard_badge()}
				</div>
				<h2 class="text-4xl md:text-5xl font-bold">{m.dashboard_title()}</h2>
				<p class="text-xl text-muted-foreground max-w-2xl mx-auto">{m.dashboard_description()}</p>
			</div>
			
			<!-- Full-width Dashboard Preview -->
			<div class="rounded-2xl overflow-hidden shadow-2xl">
				<enhanced:img src="$lib/assets/img/dashboard_main.png" alt="BetterFit Dashboard Preview" class="w-full h-full object-cover"/>
            </div>
		</div>
	</section>

	<!-- Features Showcase -->
	<section class="w-full px-4 md:px-6 py-24 bg-muted/30">
		<div class="max-w-7xl mx-auto">
			<div class="text-center space-y-4 mb-16">
				<div class="inline-block px-4 py-2 bg-primary/10 rounded-full text-sm font-medium text-primary">
					{m.features_badge()}
				</div>
				<h2 class="text-4xl md:text-5xl font-bold">{m.features_title()}</h2>
				<p class="text-xl text-muted-foreground max-w-2xl mx-auto">{m.features_description()}</p>
			</div>

			<!-- Feature 1: Member Management -->
			<div class="mb-24">
				<div class="flex flex-col lg:flex-row items-center gap-12">
					<div class="flex-1 space-y-6">
						<div class="inline-flex items-center gap-2 px-4 py-2 bg-blue-100 dark:bg-blue-950 rounded-full text-blue-600 font-medium">
							<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"/>
							</svg>
							{m.feature_members_badge()}
						</div>
						<h3 class="text-3xl md:text-4xl font-bold">{m.feature_members_title()}</h3>
						<p class="text-lg text-muted-foreground">
							{m.feature_members_description()}
						</p>
						<ul class="space-y-3">
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_members_benefit_1_title()}</p>
									<p class="text-muted-foreground">{m.feature_members_benefit_1_desc()}</p>
								</div>
							</li>
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_members_benefit_2_title()}</p>
									<p class="text-muted-foreground">{m.feature_members_benefit_2_desc()}</p>
								</div>
							</li>
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_members_benefit_3_title()}</p>
									<p class="text-muted-foreground">{m.feature_members_benefit_3_desc()}</p>
								</div>
							</li>
						</ul>
					</div>
					<div class="flex-1">
						<div class="rounded-2xl overflow-hidden shadow-2xl">
																<enhanced:img src="$lib/assets/img/dashboard_members.png" alt="Member Management Screenshot" class="w-full h-full object-cover"/>

                        </div>
					</div>
				</div>
			</div>

			<!-- Feature 2: Class Scheduling -->
			<div class="mb-24">
				<div class="flex flex-col lg:flex-row-reverse items-center gap-12">
					<div class="flex-1 space-y-6">
						<div class="inline-flex items-center gap-2 px-4 py-2 bg-cyan-100 dark:bg-cyan-950 rounded-full text-cyan-600 font-medium">
							<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
							</svg>
							{m.feature_scheduling_badge()}
						</div>
						<h3 class="text-3xl md:text-4xl font-bold">{m.feature_scheduling_title()}</h3>
						<p class="text-lg text-muted-foreground">
							{m.feature_scheduling_description()}
						</p>
						<ul class="space-y-3">
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_scheduling_benefit_1_title()}</p>
									<p class="text-muted-foreground">{m.feature_scheduling_benefit_1_desc()}</p>
								</div>
							</li>
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_scheduling_benefit_2_title()}</p>
									<p class="text-muted-foreground">{m.feature_scheduling_benefit_2_desc()}</p>
								</div>
							</li>
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_scheduling_benefit_3_title()}</p>
									<p class="text-muted-foreground">{m.feature_scheduling_benefit_3_desc()}</p>
								</div>
							</li>
						</ul>
					</div>
					<div class="flex-1">
						<div class="rounded-2xl overflow-hidden shadow-2xl">
							<enhanced:img src="$lib/assets/img/dashboard_scheduling.png" alt="Class Scheduling Screenshot" class="w-full h-full object-cover"/>
                        </div>
					</div>
				</div>
			</div>

			<!-- Feature 3: Analytics -->
			<div class="mb-24">
				<div class="flex flex-col lg:flex-row items-center gap-12">
					<div class="flex-1 space-y-6">
						<div class="inline-flex items-center gap-2 px-4 py-2 bg-green-100 dark:bg-green-950 rounded-full text-green-600 font-medium">
							<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
							</svg>
							{m.feature_analytics_badge()}
						</div>
						<h3 class="text-3xl md:text-4xl font-bold">{m.feature_analytics_title()}</h3>
						<p class="text-lg text-muted-foreground">
							{m.feature_analytics_description()}
						</p>
						<ul class="space-y-3">
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_analytics_benefit_1_title()}</p>
									<p class="text-muted-foreground">{m.feature_analytics_benefit_1_desc()}</p>
								</div>
							</li>
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_analytics_benefit_2_title()}</p>
									<p class="text-muted-foreground">{m.feature_analytics_benefit_2_desc()}</p>
								</div>
							</li>
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_analytics_benefit_3_title()}</p>
									<p class="text-muted-foreground">{m.feature_analytics_benefit_3_desc()}</p>
								</div>
							</li>
						</ul>
					</div>
					<div class="flex-1">
						<div class="rounded-2xl overflow-hidden shadow-2xl">
							<enhanced:img src="$lib/assets/img/dashboard_stats.png" alt="Analytics Screenshot" class="w-full h-full object-cover"/>
                        </div>
					</div>
				</div>
			</div>

			<!-- Feature 4: Mobile App -->
			<div>
				<div class="flex flex-col lg:flex-row-reverse items-center gap-12">
					<div class="flex-1 space-y-6">
						<div class="inline-flex items-center gap-2 px-4 py-2 bg-purple-100 dark:bg-purple-950 rounded-full text-purple-600 font-medium">
							<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z"/>
							</svg>
							{m.feature_mobile_badge()}
						</div>
						<h3 class="text-3xl md:text-4xl font-bold">{m.feature_mobile_title()}</h3>
						<p class="text-lg text-muted-foreground">
							{m.feature_mobile_description()}
						</p>
						<ul class="space-y-3">
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_mobile_benefit_1_title()}</p>
									<p class="text-muted-foreground">{m.feature_mobile_benefit_1_desc()}</p>
								</div>
							</li>
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_mobile_benefit_2_title()}</p>
									<p class="text-muted-foreground">{m.feature_mobile_benefit_2_desc()}</p>
								</div>
							</li>
							<li class="flex items-start gap-3">
								<svg class="w-6 h-6 text-green-600 mt-1 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<div>
									<p class="font-semibold">{m.feature_mobile_benefit_3_title()}</p>
									<p class="text-muted-foreground">{m.feature_mobile_benefit_3_desc()}</p>
								</div>
							</li>
						</ul>
					</div>
					<div class="flex-1">
						<div class="max-w-[300px] mx-auto">
							<div class="rounded-3xl overflow-hidden shadow-2xl">
								<enhanced:img src="$lib/assets/img/mobile_client.png" alt="Mobile App Screenshot" class="w-full h-full object-cover"/>
                            </div>
						</div>
					</div>
				</div>
			</div>
		</div>
	</section>

	<!-- Old Features Grid - Can be removed or kept as summary -->
	<section id="features" class="w-full px-4 md:px-6 py-20 scroll-mt-16">
		<div class="max-w-7xl mx-auto">
			<div class="text-center space-y-4 mb-12">
				<h2 class="text-3xl md:text-4xl font-bold">{m.overview_title()}</h2>
				<p class="text-lg text-muted-foreground">{m.overview_description()}</p>
			</div>
			<div class="grid md:grid-cols-3 gap-8">
				<div class="space-y-3 p-6 rounded-lg border bg-card">
					<div class="w-12 h-12 rounded-lg bg-blue-100 dark:bg-blue-950 flex items-center justify-center">
						<svg xmlns="http://www.w3.org/2000/svg" class="w-6 h-6 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
							<path stroke-linecap="round" stroke-linejoin="round" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
						</svg>
					</div>
					<h3 class="text-xl font-semibold">{m.overview_members_title()}</h3>
					<p class="text-muted-foreground">{m.overview_members_desc()}</p>
				</div>
				<div class="space-y-3 p-6 rounded-lg border bg-card">
					<div class="w-12 h-12 rounded-lg bg-cyan-100 dark:bg-cyan-950 flex items-center justify-center">
						<svg xmlns="http://www.w3.org/2000/svg" class="w-6 h-6 text-cyan-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
							<path stroke-linecap="round" stroke-linejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
						</svg>
					</div>
					<h3 class="text-xl font-semibold">{m.overview_scheduling_title()}</h3>
					<p class="text-muted-foreground">{m.overview_scheduling_desc()}</p>
				</div>
				<div class="space-y-3 p-6 rounded-lg border bg-card">
					<div class="w-12 h-12 rounded-lg bg-green-100 dark:bg-green-950 flex items-center justify-center">
						<svg xmlns="http://www.w3.org/2000/svg" class="w-6 h-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
							<path stroke-linecap="round" stroke-linejoin="round" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
						</svg>
					</div>
					<h3 class="text-xl font-semibold">{m.overview_analytics_title()}</h3>
					<p class="text-muted-foreground">{m.overview_analytics_desc()}</p>
				</div>
			</div>
		</div>
	</section>

	<!-- Pre-Launch Waitlist Section -->
	<section id="waitlist" class="w-full px-4 md:px-6 py-24 scroll-mt-16 bg-gradient-to-b from-muted/30 to-background">
		<div class="max-w-4xl mx-auto">
			<div class="text-center space-y-6 mb-12">
				<div class="inline-flex items-center gap-2 px-4 py-2 bg-primary/10 rounded-full text-sm font-medium text-primary">
					<span class="relative flex h-2 w-2">
						<span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-primary opacity-75"></span>
						<span class="relative inline-flex rounded-full h-2 w-2 bg-primary"></span>
					</span>
					{m.waitlist_badge()}
				</div>
				<h2 class="text-4xl md:text-5xl font-bold">{m.waitlist_title()}</h2>
				<p class="text-xl text-muted-foreground max-w-2xl mx-auto">
					{m.waitlist_description()}
				</p>
			</div>
			
			<!-- Waitlist Form -->
			<div class="max-w-2xl mx-auto">
				<Card.Root class="border-2 border-primary/20 shadow-xl">
					<Card.Content class="p-8 md:p-12">
						<form class="space-y-6">
							<div class="space-y-4">
								<div class="space-y-2">
									<Label for="email">{m.form_email_label()}</Label>
									<Input
										type="email"
										id="email"
										placeholder={m.form_email_placeholder()}
										class="h-12"
										required
									/>
								</div>
								<div class="space-y-2">
									<Label for="business">{m.form_business_label()} <span class="text-muted-foreground font-normal">{m.form_business_optional()}</span></Label>
									<Input
										type="text"
										id="business"
										placeholder={m.form_business_placeholder()}
										class="h-12"
									/>
								</div>
								<div class="space-y-2">
									<Label for="type">{m.form_type_label()}</Label>
									<Select.Root bind:selected={selectedBusinessType}>
										<Select.Trigger class="h-12 w-full">
											<span data-slot="select-value">{selectedBusinessType?.label || m.form_type_placeholder()}</span>
										</Select.Trigger>
										<Select.Content>
											<Select.Item value="gym" label={m.form_type_gym()}>{m.form_type_gym()}</Select.Item>
											<Select.Item value="personal" label={m.form_type_personal()}>{m.form_type_personal()}</Select.Item>
											<Select.Item value="yoga" label={m.form_type_yoga()}>{m.form_type_yoga()}</Select.Item>
											<Select.Item value="pilates" label={m.form_type_pilates()}>{m.form_type_pilates()}</Select.Item>
											<Select.Item value="crossfit" label={m.form_type_crossfit()}>{m.form_type_crossfit()}</Select.Item>
											<Select.Item value="dance" label={m.form_type_dance()}>{m.form_type_dance()}</Select.Item>
											<Select.Item value="martial" label={m.form_type_martial()}>{m.form_type_martial()}</Select.Item>
											<Select.Item value="pool" label={m.form_type_pool()}>{m.form_type_pool()}</Select.Item>
											<Select.Item value="other" label={m.form_type_other()}>{m.form_type_other()}</Select.Item>
										</Select.Content>
									</Select.Root>
								</div>
							</div>
							
							<Button
								type="submit"
								size="lg"
								class="w-full h-14 text-lg shadow-xl"
							>
								{m.form_submit()}
								<svg class="ml-2 w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6"/>
								</svg>
							</Button>
							
							<p class="text-xs text-center text-muted-foreground">
								{m.form_privacy()}
							</p>
						</form>
						
						<!-- Benefits List -->
						<div class="mt-10 pt-8 border-t">
						<p class="text-sm font-semibold mb-4 text-center">{m.benefits_title()}</p>
						<ul class="space-y-3">
							<li class="flex items-start gap-3 text-sm">
								<svg class="w-5 h-5 text-green-600 mt-0.5 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<span>{@html m.benefit_1()}</span>
							</li>
							<li class="flex items-start gap-3 text-sm">
								<svg class="w-5 h-5 text-green-600 mt-0.5 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<span>{@html m.benefit_2()}</span>
							</li>
							<li class="flex items-start gap-3 text-sm">
								<svg class="w-5 h-5 text-green-600 mt-0.5 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<span>{@html m.benefit_3()}</span>
							</li>
							<li class="flex items-start gap-3 text-sm">
								<svg class="w-5 h-5 text-green-600 mt-0.5 shrink-0" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"/>
								</svg>
								<span>{@html m.benefit_4()}</span>
							</li>
						</ul>
						</div>
					</Card.Content>
				</Card.Root>
			</div>
		</div>
	</section>

	<!-- About Section -->
	<section id="about" class="w-full px-4 md:px-6 py-20 scroll-mt-16">
		<div class="max-w-7xl mx-auto">
			<div class="max-w-3xl mx-auto text-center space-y-6">
			<h2 class="text-3xl md:text-4xl font-bold">{m.about_title()}</h2>
			<p class="text-lg text-muted-foreground">
				{m.about_description()}
			</p>
			</div>
		</div>
	</section>

	<!-- Footer -->
	<footer class="w-full border-t border-border/40 bg-muted/30">
		<div class="max-w-7xl mx-auto px-4 md:px-6">
			<!-- Main Footer Content -->
			<div class="py-16 grid grid-cols-1 md:grid-cols-4 gap-12">
				<!-- Brand Column -->
				<div class="space-y-4">
					<div class="flex items-center gap-2">
						<div class="flex items-center justify-center w-9 h-9 rounded-lg bg-gradient-to-br from-primary to-secondary">
							<svg
								xmlns="http://www.w3.org/2000/svg"
								viewBox="0 0 24 24"
								fill="none"
								stroke="currentColor"
								stroke-width="2"
								stroke-linecap="round"
								stroke-linejoin="round"
								class="w-5 h-5 text-white"
							>
								<path d="M14 14.5c0 1.4-1.1 2.5-2.5 2.5S9 15.9 9 14.5s1.1-2.5 2.5-2.5 2.5 1.1 2.5 2.5z"/>
								<path d="M8.5 8.5c0 1.4-1.1 2.5-2.5 2.5S3.5 9.9 3.5 8.5 4.6 6 6 6s2.5 1.1 2.5 2.5z"/>
								<path d="M20 8.5c0 1.4-1.1 2.5-2.5 2.5S15 9.9 15 8.5 16.1 6 17.5 6s2.5 1.1 2.5 2.5z"/>
								<line x1="6" y1="11" x2="9" y2="12"/>
								<line x1="15" y1="12" x2="18" y2="11"/>
								<line x1="12" y1="17" x2="12" y2="21"/>
							</svg>
						</div>
						<span class="font-bold text-xl text-primary">BetterFit</span>
					</div>
					<p class="text-sm text-muted-foreground max-w-xs">
						{m.footer_brand_tagline()}
					</p>
				</div>

				<!-- Product Column -->
				<div class="space-y-4">
					<h3 class="font-semibold text-sm">{m.footer_product()}</h3>
					<ul class="space-y-3 text-sm text-muted-foreground">
						<li>
							<a href="#features" class="hover:text-foreground transition-colors">{m.footer_features()}</a>
						</li>
						<li>
							<a href="#waitlist" class="hover:text-foreground transition-colors">{m.footer_waitlist()}</a>
						</li>
						<li>
							<a href="#about" class="hover:text-foreground transition-colors">{m.footer_about()}</a>
						</li>
					</ul>
				</div>

				<!-- Company Column -->
				<div class="space-y-4">
					<h3 class="font-semibold text-sm">{m.footer_company()}</h3>
					<ul class="space-y-3 text-sm text-muted-foreground">
						<li>
							<a href="#about" class="hover:text-foreground transition-colors">{m.footer_our_story()}</a>
						</li>
						<li>
							<span class="text-muted-foreground/50 cursor-not-allowed">{m.footer_careers()}</span>
						</li>
						<li>
							<span class="text-muted-foreground/50 cursor-not-allowed">{m.footer_blog()}</span>
						</li>
					</ul>
				</div>

				<!-- Legal Column -->
				<div class="space-y-4">
					<h3 class="font-semibold text-sm">{m.footer_legal()}</h3>
					<ul class="space-y-3 text-sm text-muted-foreground">
						<li>
							<span class="text-muted-foreground/50 cursor-not-allowed">{m.footer_privacy()}</span>
						</li>
						<li>
							<span class="text-muted-foreground/50 cursor-not-allowed">{m.footer_terms()}</span>
						</li>
					</ul>
				</div>
			</div>

			<!-- Bottom Bar -->
			<div class="py-6 border-t border-border/40">
				<div class="flex flex-col md:flex-row justify-between items-center gap-4">
					<p class="text-sm text-muted-foreground">
						{m.footer_copyright({ year: new Date().getFullYear() })}
					</p>
					<div class="flex items-center gap-6">
						<!-- Social Links -->
						<button type="button" class="text-muted-foreground hover:text-foreground transition-colors" aria-label="Twitter">
							<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
								<path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
							</svg>
						</button>
						<button type="button" class="text-muted-foreground hover:text-foreground transition-colors" aria-label="LinkedIn">
							<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
								<path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/>
							</svg>
						</button>
						<button type="button" class="text-muted-foreground hover:text-foreground transition-colors" aria-label="Instagram">
							<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
								<path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zM12 0C8.741 0 8.333.014 7.053.072 2.695.272.273 2.69.073 7.052.014 8.333 0 8.741 0 12c0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98C8.333 23.986 8.741 24 12 24c3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98C15.668.014 15.259 0 12 0zm0 5.838a6.162 6.162 0 100 12.324 6.162 6.162 0 000-12.324zM12 16a4 4 0 110-8 4 4 0 010 8zm6.406-11.845a1.44 1.44 0 100 2.881 1.44 1.44 0 000-2.881z"/>
							</svg>
						</button>
					</div>
				</div>
			</div>
		</div>
	</footer>
</div>