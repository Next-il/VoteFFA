# Vote FFA

## Dependencies
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)
- [Next-il/Votes](https://github.com/Next-il/Votes) v1 or newer: `plugins/Votes` and `shared/VotesApi`. `css_ffa` starts its
  vote through `VotesApi`, so the vote card, F1/F2 and the `sv_allow_votes` window all come from Votes. Without it the
  command tells the player votes are unavailable.

## Configuration

Config Located In `addons\counterstrikesharp\plugins\configs\VoteFFA\VoteFFA.json`
>

```json
{
  // Delay between votes
  "Delay": 25,

  // Vote duration
  "VoteDuration": 30,

  // Passes when yes reaches this % of everyone who can vote, and beats no
  "PassPercent": 51,
}
```

## Language:
```json
{
	//==========================
	//        Colors
	//==========================
	//{Yellow} {Gold} {Silver} {Blue} {DarkBlue} {BlueGrey} {Magenta} {LightRed}
	//{LightBlue} {Olive} {Lime} {Red} {Purple} {Grey}
	//{Default} {White} {Darkred} {Green} {LightYellow}
	//==========================
	//        Other
	//==========================
	//<br> = Next Line On Center Hud 
	//{nextline} = Print On Next Line
	//==========================
	
	
	"prefix": "{lime}➜ {grey}FFA {lime}|{default}",
	"vote.enable.title": "Enable FFA?",
	"vote.disable.title": "Disable FFA?",
	"vote.already-in-progress": "A vote is already in progress.",
	"vote.answers.yes": "Yes",
	"vote.answers.no": "No",
	"player.already-voted": "You have already voted.",
	"player.vote-success": "You have voted {lime}{0}{default}.",
	"vote.success": "The vote has passed, {lime}FFA{default} will be activated from the next round.",
	"vote.failed": "The vote has failed (not enough votes).",
	"ffa.state.enabled": "FFA is enabled! Type {lime}!ffa{default} to disable it.",
	"vote.delay": "You must wait {lime}{0}{default} seconds before starting a new vote."
}
```
