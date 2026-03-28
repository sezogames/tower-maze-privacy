# TowerMaze ASO Measurement & Tracking Plan

## Tools

| Tool | Purpose | Setup |
|------|---------|-------|
| Google Play Console | Store listing experiments, acquisition reports, search terms | Auto with dev account |
| App Store Connect | Product page optimization, impression sources | Auto with dev account |
| Google Search Console | Landing page search performance | Register towermaze.game |
| Bing Webmaster Tools | Landing page search (Bing/Yandex) | Register towermaze.game |
| AppFollow (free tier) | Keyword ranking tracking | Sign up, add app |

## Key Metrics & Targets

| Metric | Target | Source |
|--------|--------|--------|
| Store conversion rate (impressions → installs) | >30% organic | Play Console / ASC |
| Keyword ranking position (top 20 keywords) | Track weekly | AppFollow |
| Landing page CTR | Track baseline | Search Console |
| Social media referral installs | Track via UTM | UTM-tagged store links |

## Post-Launch Optimization Schedule

| Timeframe | Action |
|-----------|--------|
| Week 1-2 | Collect baseline: impressions, conversion rate, top search terms |
| Week 3 | First Google Play store listing experiment (icon or screenshot A/B test) |
| Week 4 | Revise keywords based on actual search term data from both stores |
| Month 2 | Screenshot A/B test round 2, description optimization |
| Monthly | Review keyword rankings, update long-tail keywords, refresh social content |

## UTM Link Template

Use these UTM parameters for all social media store links:

- Google Play: `https://play.google.com/store/apps/details?id=com.sezogames.towermaze&utm_source={platform}&utm_medium=social&utm_campaign=launch`
- App Store: Use App Store campaign tokens via App Store Connect

Replace `{platform}` with: `tiktok`, `instagram`, `youtube`, `twitter`, `facebook`

## Registration Checklist

- [ ] Register towermaze.game in Google Search Console
- [ ] Register towermaze.game in Bing Webmaster Tools
- [ ] Create AppFollow account and add TowerMaze
- [ ] Set up UTM-tagged links for each social platform
- [ ] Verify landing page is indexed by Google (after deployment)
