document.addEventListener('DOMContentLoaded', () => {
    const element = document.querySelector('#yourElementId');
    if (element) {
        element.classList.add('your-class');
    } else {
        console.warn('Element with #yourElementId not found.');
    }
});
