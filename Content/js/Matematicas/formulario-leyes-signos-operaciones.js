/* formulario-leyes-signos-operaciones.js */
(function () {
    'use strict';

    /* Steppers */
    document.querySelectorAll('.stepper__btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var input = document.querySelector('[name="' + btn.dataset.target + '"]');
            if (!input) return;
            var val = parseInt(input.value, 10) || 0;
            var nuevo = Math.min(parseInt(input.max), Math.max(parseInt(input.min), val + parseInt(btn.dataset.dir)));
            input.value = nuevo;
            if (btn.dataset.target === 'MinDigitos' || btn.dataset.target === 'MaxDigitos')
                sincronizarDigitos();
        });
    });

    function sincronizarDigitos() {
        var mi = document.querySelector('[name="MinDigitos"]');
        var ma = document.querySelector('[name="MaxDigitos"]');
        if (!mi || !ma) return;
        if (parseInt(ma.value) < parseInt(mi.value)) ma.value = mi.value;
        if (parseInt(mi.value) > parseInt(ma.value)) mi.value = ma.value;
    }

    /* Speed pills */
    var hd = document.getElementById('hdVelocidad');
    document.querySelectorAll('.speed-pill').forEach(function (pill) {
        pill.addEventListener('click', function () {
            document.querySelectorAll('.speed-pill').forEach(function (p) { p.classList.remove('speed-pill--active'); });
            pill.classList.add('speed-pill--active');
            if (hd) hd.value = pill.dataset.val;
        });
    });

    /* Tipo cards — activar y mostrar hints */
    var hintOperandos = document.getElementById('hintOperandos');
    var hintRaiz = document.getElementById('hintRaiz');
    var hintExponente = document.getElementById('hintExponente');
    var grupoOperandos = document.getElementById('grupoCantOperandos');

    function actualizarHints(val) {
        // val: 0=PuroSigno,1=Lit,2=Reales,3=Parent,4=Exp,5=Alea,6=Corch,7=Raiz
        var esRaiz = val === '7';
        var esExp = val === '4';
        var esPuro = val === '0';

        if (hintOperandos) hintOperandos.style.display = (!esRaiz && !esExp) ? 'block' : 'none';
        if (hintRaiz) hintRaiz.style.display = esRaiz ? 'block' : 'none';
        if (hintExponente) hintExponente.style.display = esExp ? 'block' : 'none';

        // Operandos bloqueados en 1 para raíz y exponente
        var inputOp = document.querySelector('[name="CantidadOperandos"]');
        if (inputOp) {
            if (esRaiz || esExp) {
                inputOp.value = 1;
                if (grupoOperandos) grupoOperandos.style.opacity = '0.4';
                document.querySelectorAll('[data-target="CantidadOperandos"]').forEach(function (b) {
                    b.disabled = true;
                });
            } else {
                if (parseInt(inputOp.value) < 2) inputOp.value = 2;
                if (grupoOperandos) grupoOperandos.style.opacity = '1';
                document.querySelectorAll('[data-target="CantidadOperandos"]').forEach(function (b) {
                    b.disabled = false;
                });
            }
        }
    }

    document.querySelectorAll('.tipo-card input[type="radio"]').forEach(function (radio) {
        radio.addEventListener('change', function () {
            document.querySelectorAll('.tipo-card').forEach(function (c) { c.classList.remove('tipo-card--active'); });
            radio.closest('.tipo-card').classList.add('tipo-card--active');
            actualizarHints(radio.value);
        });
        // Estado inicial
        if (radio.checked) actualizarHints(radio.value);
    });

})();
