// Scroll carousel to a specific index
window.scrollCarouselTo = function (carouselElement, index) {
    if (carouselElement) {
        const items = carouselElement.querySelectorAll('.image-container');
        if (items.length > index) {
            const itemWidth = items[0].offsetWidth;
            carouselElement.scrollLeft = itemWidth * index;
        }
    }
};

// Initialize touch event handlers for pinch-zoom
window.initPinchZoom = function (element) {
    if (element) {
        element.addEventListener('touchstart', function(e) {
            if (e.touches.length === 2) {
                e.preventDefault();
            }
        });

        element.addEventListener('touchmove', function(e) {
            if (e.touches.length === 2) {
                e.preventDefault();
            }
        });
    }
};
