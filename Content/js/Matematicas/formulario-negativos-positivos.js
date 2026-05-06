/* ============================================================
   formulario-negativos-positivos.js
   Ruta: Content/js/Matematicas/formulario-negativos-positivos.js
   ============================================================ */
(function () {
    'use strict';

    /* ── Steppers ─────────────────────────────────────────── */
    document.querySelectorAll('.stepper__btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var input = document.querySelector('[name="' + btn.dataset.target + '"]');
            if (!input) return;
            var val = parseInt(input.value, 10) || 0;
            var nuevo = Math.min(parseInt(input.max), Math.max(parseInt(input.min), val + parseInt(btn.dataset.dir)));
            input.value = nuevo;
            if (btn.dataset.target === 'MinDigitos' || btn.dataset.target === 'MaxDigitos') {
                sincronizarDigitos();
                actualizarHint();
            }
        });
    });

    function sincronizarDigitos() {
        var mi = document.querySelector('[name="MinDigitos"]');
        var ma = document.querySelector('[name="MaxDigitos"]');
        if (!mi || !ma) return;
        if (parseInt(ma.value) < parseInt(mi.value)) ma.value = mi.value;
        if (parseInt(mi.value) > parseInt(ma.value)) mi.value = ma.value;
    }

    function actualizarHint() {
        var mi = document.querySelector('[name="MinDigitos"]');
        var ma = document.querySelector('[name="MaxDigitos"]');
        var hint = document.getElementById('hintRecta');
        if (!mi || !ma || !hint) return;
        hint.style.display = (parseInt(mi.value) === 1 && parseInt(ma.value) === 1) ? 'block' : 'none';
    }
    actualizarHint();

    /* ── Speed pills ──────────────────────────────────────── */
    var hd = document.getElementById('hdVelocidad');
    document.querySelectorAll('.speed-pill').forEach(function (pill) {
        pill.addEventListener('click', function () {
            document.querySelectorAll('.speed-pill').forEach(function (p) {
                p.classList.remove('speed-pill--active');
            });
            pill.classList.add('speed-pill--active');
            if (hd) hd.value = pill.dataset.val;
        });
    });

})();