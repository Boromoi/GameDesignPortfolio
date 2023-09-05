/*---- Get elements -----*/
const navbarToggleButton = document.getElementById("navbar-toggle-button");
const navbarBackground = document.getElementById("navbar-bg");
const navbarContact = document.getElementById("navbar-contact");

/*---- Variables -----*/
var navbarToggleButtonPressedAmount = 0;
var navbarToggleButtonIsActive = new Boolean;

// Function to change the navbar background when the navbarToggleButton get's pressed.
function NavbarToggleButtonPressed() {
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

/*
console.log(document.referrer);

function getElementY(query) {
    return window.pageYOffset + document.querySelector(query).getBoundingClientRect().top
}

function DoScrolling(elementY, duration) {
    //if (document.referrer.includes == "/") {
    //    window.location.href = "/";
    //};

    var startingY = window.pageYOffset;
    var diff = elementY - startingY;
    var start;

    // Bootstrap our animation - it will get called right before next frame shall be rendered.
    window.requestAnimationFrame(function step(timestamp) {
        if (!start) start = timestamp;
        // Elapsed milliseconds since start of scrolling.
        var time = timestamp - start;
        // Get percent of completion in range [0, 1].
        var percent = Math.min(time / duration, 1);

        window.scrollTo(0, startingY + diff * percent);

        // Proceed with animation as long as we wanted it to.
        if (time < duration) {
            window.requestAnimationFrame(step);
        }
    })
}

navbarContact.addEventListener('click', DoScrolling.bind((window.scrollY, 1)));*/

function scrollFunction(scrollTo) {
    const element = document.getElementById(scrollTo);
    element.scrollIntoView({ behavior: 'smooth' });
}