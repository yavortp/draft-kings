# QA Automation Assignment Submission

## Test Planning

Test plan was designed around the provided MD files and some additional cases for better covarage on negative testing and eventual edge cases.

### Test Structure

API tests are using centralized ApiHelpers class which handles HTTP calls and parcing.
BetDTO is created to easily transfer data Objects between classes.
UI test cases are using BetSlipPagePOM.cs for all locators and common methods. Test stay readable and have clean structure.
PollUntilAsync function is reused where applicable to handle state transitions.

#### Defects

1. BetPlacementTests.Placing_A_Bet_With_Negative_Stake_Returns_400 - case not handled correctly, there is no console error, nor there is a UI error. Bad user experience. 

2. BetPlacementTests.Placing_A_Bet_With_Stake_Less_Than_1_cent_Returns_400 - input field accepts values less than 0.01 (menaining number with more than 2 decimal places). Bet goes through.

3. BetPlacementTests.Bet_Input_Field_Should_Return_400_If_Letters_Or_Special_Chars_Are_Entered - input field accepts letter "e". All other letters and special characters are not allowed for input at all. Bet doesnt go throug, bad UX.

