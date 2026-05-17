// Google Maps Places Autocomplete for venue name search.
// Loaded dynamically — only on VenueEdit page.
window.venueAutocomplete = (function () {
    let _ready = false;

    function setup(dotnetRef, inputId) {
        const input = document.getElementById(inputId);
        if (!input || _ready) return;
        _ready = true;

        const ac = new google.maps.places.Autocomplete(input, {
            // 'establishment' finds sports venues, clubs, etc. by name
            types: ['establishment'],
            componentRestrictions: { country: 'br' },
            fields: ['name', 'address_components'],
        });

        ac.addListener('place_changed', () => {
            const place = ac.getPlace();
            if (!place || !place.address_components) return;

            const name = place.name || '';
            let streetNumber = '', route = '', sublocality = '', city = '', state = '';
            for (const comp of place.address_components) {
                const t = comp.types;
                if      (t.includes('street_number'))               streetNumber = comp.long_name;
                else if (t.includes('route'))                       route        = comp.long_name;
                else if (t.includes('sublocality_level_1'))         sublocality  = comp.long_name;
                else if (t.includes('locality'))                    city         = comp.long_name;
                else if (t.includes('administrative_area_level_2') && !city) city = comp.long_name;
                else if (t.includes('administrative_area_level_1')) state        = comp.short_name;
            }

            let street = route;
            if (streetNumber) street += street ? `, ${streetNumber}` : streetNumber;
            if (sublocality)  street += street ? ` — ${sublocality}` : sublocality;

            dotnetRef.invokeMethodAsync('PlaceSelected', name, street, city, state);
        });
    }

    return {
        init: function (apiKey, dotnetRef, inputId) {
            if (window.google?.maps?.places) {
                setup(dotnetRef, inputId);
                return;
            }

            // Queue callback — safe against multiple calls before script loads
            window.__venueMapSetup = () => setup(dotnetRef, inputId);

            if (!document.getElementById('google-maps-js')) {
                const s = document.createElement('script');
                s.id   = 'google-maps-js';
                s.src  = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&libraries=places&callback=__venueMapSetup&language=pt-BR`;
                s.async = true;
                s.defer = true;
                document.head.appendChild(s);
            }
        },

        dispose: function () {
            _ready = false;
        },
    };
})();
