/*---- Get elements -----*/
const navbarToggleButton = document.getElementById("navbar-toggle-button");
const navbarBackground = document.getElementById("navbar-bg");

/*---- Variables -----*/
var navbarToggleButtonPressedAmount = 0;
var navbarToggleButtonIsActive = new Boolean;

// Function to change the navbar background when the navbarToggleButton get's pressed.
function navbarToggleButtonPressed() {
    navbarToggleButtonPressedAmount++;

    // Check how many times the button has been pressed and set the button on active or false depending on the amount.
    if (navbarToggleButtonPressedAmount % 2 == 0) {
        navbarToggleButtonIsActive = false;
    } else {
        navbarToggleButtonIsActive = true;
    }

    // Change the background height depending on if the button is active or not.
    if (navbarToggleButtonIsActive) {
        // set background heigth to 108px
        navbarBackground.style.setProperty('--navbar-background-height', '100px');
    } else { 
        // set background heigth to 60px
        navbarBackground.style.setProperty('--navbar-background-height', '60px');
    }
}