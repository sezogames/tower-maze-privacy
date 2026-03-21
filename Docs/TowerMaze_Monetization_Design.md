# Tower Maze Monetization Design

## Goal
Build a monetization loop that increases retention first, then ad revenue, then IAP conversion.

Core rule:
- The player should feel pressure from the game, not from monetization.
- Ads should appear as a player choice when possible.
- IAP should reduce friction or add style, not break the skill loop.

## Product Positioning
This game fits a `hybrid-casual` model:
- short sessions
- high retry count
- skill + memorization loop
- timer-driven events
- strong rewarded ad potential
- cosmetic and light utility IAP potential

## Monetization Pillars
1. Rewarded ads as the primary ad format.
2. Soft currency economy for retention and shop usage.
3. Cosmetic-heavy store with light utility items.
4. Timed events and missions to create return reasons.
5. Very light interstitial and app open usage.

## Success Metrics
Primary:
- D1 retention
- D7 retention
- average session length
- runs per session
- rewarded opt-in rate
- continue usage rate
- ARPDAU
- payer conversion

Secondary:
- mission completion rate
- event participation rate
- 30 second height
- 60 second height
- store open rate
- first purchase time

## Economy Overview
### Currency
Use one soft currency only: `Ember`.

Why:
- easier to understand
- easier to balance
- faster to ship
- enough for early shop and events

### Ember Sources
- run result reward
- daily mission reward
- weekly event reward
- free daily chest
- rewarded `x2 Ember`
- limited streak rewards

### Run Reward Formula
Recommended first version:

`Ember = floor(height * 1.25) + (zoneReached * 15) + timeBonus + missionBonus`

Suggested time bonus:
- under 30s and reaches target event height: `+20`
- under 60s and reaches target event height: `+40`

Suggested fail floor:
- minimum reward per meaningful run: `10`

### Rewarded Multiplier
Fail screen:
- `x2 Ember` button
- available every run
- should not stack with continue reward

Suggested choice order on fail screen:
1. Continue
2. x2 Ember
3. Retry

## Shop Design
### Shop Sections
- Skins
- Trails
- Death FX
- Utility
- Bundles

### Cosmetics
Examples:
- Obsidian Ball
- Molten Core Ball
- Ash Marble Ball
- Neon Hazard Ball
- Rune Trail
- Heat Trail
- Ember Burst death effect

These should be the main long-term sink for Ember.

### Utility Items
Keep these controlled and non-abusive:
- Continue Ticket: adds one extra continue for a run
- Event Retry Token: restarts an event attempt without losing that event slot
- Rush Shield: protects from one rush burst for a short time
- Flip Guard: negates one control flip event

Important:
- do not sell permanent stat upgrades that trivialize the maze
- do not increase base movement speed through monetization
- do not sell unlimited revives

### Suggested Price Bands
Soft currency:
- Common skin: `300-500 Ember`
- Rare skin: `800-1200 Ember`
- Epic skin: `1800-2500 Ember`
- Trail: `500-900 Ember`
- FX: `700-1200 Ember`
- Utility single-use item: `100-250 Ember`

IAP:
- Remove Ads: low-price evergreen SKU
- Starter Pack: low-price intro pack
- Small Ember Pack
- Medium Ember Pack
- Large Ember Pack
- Cosmetic Bundle Pack

## Ad Strategy
### Primary Ad Placements
Rewarded ads only at first:
- Continue
- x2 Ember
- Daily chest instant open
- Mission reroll
- Event retry
- Free utility item

### Secondary Placements
Use carefully:
- App Open ad
- Interstitial or rewarded interstitial

### Rewarded Continue
Already fits the current game.

Rules:
- once per run by default
- strong value
- only after fail
- do not hide Retry behind it

### Rewarded x2 Ember
This should become the second strongest placement.

Rules:
- always available after fail
- not shown after the player chooses Continue until the next fail or run end
- clear reward text

### Daily Chest
Loop:
- one free chest per day
- optional rewarded open for instant second chest
- contents: Ember, utility item, cosmetic fragment

### Mission Reroll
Good rewarded usage:
- one free reroll per day
- extra rerolls via rewarded ad

### Interstitial Strategy
Recommended first release:
- no interstitial during the first few sessions
- no interstitial before the player completes several runs
- max one every `3-4` fails
- no interstitial immediately after a rewarded ad
- no interstitial before the first run of the session

### App Open Strategy
Recommended:
- disable for first-time users
- only after several app launches
- only on natural return/loading moments
- never before the player regains context

## IAP Strategy
### Launch IAP Set
1. Remove Ads
2. Starter Pack
3. Small Ember Pack
4. Medium Ember Pack
5. Large Ember Pack
6. Cosmetic Bundle

### Remove Ads
Removes:
- interstitial
- app open

Does not remove:
- rewarded ads

This should be the simplest and most stable SKU.

### Starter Pack
Recommended content:
- Ember
- 1 exclusive skin
- 3 Continue Tickets
- 1 Event Retry Token

Recommended behavior:
- show only after the player has felt friction and value
- first offer after first continue usage or after reaching mid-game height once

### Currency Packs
Purpose:
- support cosmetic purchases
- support event retry economy

Do not let currency packs dominate progression.

### Cosmetics Over Power
Long-term revenue should come more from cosmetics than power.
This game is skill-driven. If monetization breaks fairness, retention will collapse.

## Retention Systems
### Daily Missions
Examples:
- Reach `18m` in `30s`
- Reach `Zone 3`
- Use `0` continues and hit `20m`
- Watch `1` rewarded ad
- Survive `2` rush events

Reward:
- Ember
- utility items
- cosmetic fragments

### Weekly Events
Best fit for this game.

Examples:
- `30 seconds to 20m`
- `45 seconds to Zone 4`
- `No continue challenge`
- `Rush-heavy challenge`
- `Control flip challenge`

Event traits:
- shared event seed
- fixed timer rules
- separate leaderboard
- limited retries or retry tokens

### Streak Rewards
Simple and effective:
- Day 1: Ember
- Day 2: Ember
- Day 3: utility item
- Day 5: cosmetic fragment
- Day 7: premium-feeling reward

### Collection Loop
Add long-term goals:
- skin sets
- badge unlocks
- event frames
- trail collection
- death FX collection

## Event System Recommendation
Your timer addition makes event design straightforward.

Recommended event model:
- each event has a timer target
- each event has a height target
- each event has rule modifiers
- event score = height + time efficiency + no-continue bonus

Suggested event archetypes:
- Speed Climb
- Rush Survival
- Flip Control
- No Revive
- Precision Maze

## Leaderboard Plan
### Current State
Current local leaderboard rule:
- highest height first
- fastest time as tie-break

### Next Stage
Add two leaderboard types:
- Local Best
- Event Leaderboard

Recommended later server fields:
- playerId
- eventId
- scoreHeight
- scoreTime
- runSeed
- continueUsed
- version

## Player Journey
### Session 1-3
Focus:
- learn controls
- hit first difficulty wall
- feel improvement

Monetization:
- rewarded continue only
- no aggressive ads
- no app open
- no interstitial

### Session 4-7
Focus:
- missions
- first cosmetic desire
- first event participation

Monetization:
- x2 Ember
- starter pack
- very light app open or interstitial tests

### Session 8+
Focus:
- event competition
- collection
- efficiency builds

Monetization:
- bundles
- remove ads
- repeat rewarded usage

## UX Rules
Do:
- make every ad opt-in when possible
- put value text on every monetized button
- keep Retry always easy to access
- cap frustration moments

Do not:
- chain ads after fail
- force ad before first run
- hide gameplay behind payment
- use unclear reward wording

## Platform and Policy Guardrails
For iOS and Android:
- digital goods must use platform billing
- rewarded ads must be user-initiated
- app open ads should be shown carefully
- privacy consent flow must be integrated

Implementation implication:
- use Apple IAP for iOS digital goods
- use Google Play Billing for Android digital goods
- use UMP for consent
- use ATT prompt flow if tracking is used on iOS

## Recommended Implementation Order
### Phase 1
- Ember currency
- run reward
- x2 Ember rewarded button
- local shop shell
- cosmetic inventory save

### Phase 2
- Remove Ads
- Starter Pack
- currency packs
- utility items

### Phase 3
- daily missions
- daily chest
- weekly event framework

### Phase 4
- event leaderboard backend
- A/B test ad timing
- live ops tuning

## Concrete First Release Scope
If shipping soon, build only this:
- Ember
- x2 Ember rewarded
- Continue rewarded
- Remove Ads
- Starter Pack
- 6-10 cosmetics
- 3 daily missions
- 1 weekly event mode
- local leaderboard

This is enough to validate:
- rewarded opt-in
- retention lift from missions
- purchase intent from cosmetics
- starter pack conversion

## Recommended Defaults For This Project
- Rewarded continue: `1x per run`
- Rewarded x2 Ember: `1x per fail`
- Daily missions: `3`
- Free daily chest: `1`
- Interstitial cap: `max 1 per 4 fails`
- App open: disabled until user has at least `3` sessions
- Starter pack timing: after first meaningful fail wall

## Questions To Answer Before Full Implementation
- Will utility items be usable in normal runs, events, or both?
- Will event leaderboards be local only at first, or server-backed?
- Should cosmetic unlocks be permanent account-level inventory only?
- Should Remove Ads also include a small Ember grant?

## Build Recommendation
The best next build is:
- local Ember economy
- fail screen `x2 Ember`
- shop with 6 cosmetics
- Remove Ads SKU
- Starter Pack SKU
- 3 daily missions

That is the smallest monetization version that can actually be measured.
