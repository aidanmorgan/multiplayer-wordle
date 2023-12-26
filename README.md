# Multiplayer Wordle

A verion of [Wordle](https://www.nytimes.com/games/wordle/index.html) that supports multiple players at the same time.

# How to play

When a game is active, users submit words that they want to play, or vote on the words provided by other users. At the end of a timeout the word with the highest number of votes is submitted as the chosen word and the game state is updated. There are multiple configuration options that fine-tune how the game is to be played.

# Core Model

## Tenant

Allows multiple games to be played in parallel, scoping all operations for a game to be isolated from each other.

## Session

An active game of wordle, there can only be one active Session for each Tenant. 

A Session has multiple states:
* `ACTIVE` - The Session is currently active for the Tenant
* `INACTIVE` - The Session is not the active Session for the Tenant
* `SUCCESS` - The Session has completed and the players guessed the word correctly.
* `FAIL` - The Session has completed and the players did not guess the word correctly.
* `TERMINATED` - The Session has been forcibly made inactive due to timeouts or the tenant-owner cancelled it.

## Round

A round is the curret "row" of the Wordle game that is being played by players.

A Round has multiple states:
* `ACTIVE` - The current "row" of the Session that players are playing.
* `INACTIVE` - A previous "row" of the Session that players have previously played.
* `TERMINATED` - The "row" was terminated due to the Session being terminated.


# Configuration Options

The configuration for each game (called a `Session` in the codebase) allows the following settings to be configured:

* `Number of Rounds` - The number of rounds to be played.
* `Dictionary Name` - The name of the dictionary to use (as defined in `DynamoDictionaryLoader`
* `Word Length` - The number of letters in the word to be played (as defined in `DynamoDictionaryLoader`)
* `Initial Round Length` - The number of seconds before the first check for a minimum number of votes for the round.
* `Round Extension Window` - If a new word, or a vote, comes in within this many seconds of the end of a round then the round is extended.
* `Round Extension Length` - If a new word, or a vote, comes in within the `Round Extension Window` then the round expiry time is extended by this amount.
* `Maximum Round Extensions` - The maximum number of times a round can be extended before the game is cancelled.
* `Minimum Answers Required` - The minimum number of words, or votes, that are required before a round qualifies to be ended. Any less than this will cause the round to extend.
* `Round Votes Per User` - Then number of words, or votes, that a user is allowed to submit per round.
* `Tiebreaker Strategy` - If the criteria to end a round is met, but there are words with the same number of votes then how to select the word for the round. Options include: `RANDOM`, `FIRST_IN`, `LAST_IN`
* `Maximum Round Duration` - The maximum amount of time a round is allowed to be `ACTIVE` for before the game is forced into a `TERMINATED` state
