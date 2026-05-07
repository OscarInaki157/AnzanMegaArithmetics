/* formulario-leyes-signos-operaciones.js */
(function () {
    'use strict';

    /* ── Steppers ── */
    document.querySelectorAll('.stepper__btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            if (btn.disabled) return;
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

    /* ── Speed pills ── */
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

    /* ── Tipo cards — checkboxes múltiples ── */
    /* Estrategia: escuchar SOLO el checkbox, nunca el click de la tarjeta */

    function contarSeleccionados() {
        return document.querySelectorAll('.tipo-card input[type="checkbox"]:checked').length;
    }

    document.querySelectorAll('.tipo-card').forEach(function (card) {
        var chk = card.querySelector('input[type="checkbox"]');
        if (!chk) return;

        // Sincronizar estado visual inicial
        card.classList.toggle('tipo-card--active', chk.checked);

        // Al hacer click en la tarjeta: toggle del checkbox
        card.addEventListener('click', function (e) {
            // Si el click fue directamente sobre el checkbox, no hacer nada extra
            // (el navegador ya cambió chk.checked antes de llegar aquí en algunos browsers)
            // Usamos setTimeout para leer el estado DESPUÉS del comportamiento nativo
            setTimeout(function () {
                // Si quedó desmarcado y era el último, volver a marcar
                if (!chk.checked && contarSeleccionados() === 0) {
                    chk.checked = true;
                }
                card.classList.toggle('tipo-card--active', chk.checked);
                actualizarHints();
            }, 0);
        });
    });

    function actualizarHints() {
        var checks = document.querySelectorAll('.tipo-card input[type="checkbox"]:checked');
        var vals = Array.from(checks).map(function (c) { return parseInt(c.value); });

        var hintMulti = document.getElementById('hintMultiTipo');
        var hintRaizExp = document.getElementById('hintRaizExp');
        var hintOp = document.getElementById('hintOperandos');
        var inputOp = document.querySelector('[name="CantidadOperandos"]');
        var grupoOp = document.getElementById('grupoCantOperandos');

        if (hintMulti) hintMulti.style.display = vals.length > 1 ? 'block' : 'none';

        // Solo raíz (7) o solo exponente (4) → bloquear operandos
        var soloRaizExp = vals.length === 1 && (vals[0] === 7 || vals[0] === 4);
        if (hintRaizExp) hintRaizExp.style.display = soloRaizExp ? 'block' : 'none';
        if (hintOp) hintOp.style.display = soloRaizExp ? 'none' : 'block';
        if (grupoOp) grupoOp.style.opacity = soloRaizExp ? '0.4' : '1';

        document.querySelectorAll('[data-target="CantidadOperandos"]').forEach(function (b) {
            b.disabled = soloRaizExp;
        });
    }

    /* ── Operaciones: botones visuales que actualizan hiddens ── */
    var opConfig = [
        { labelId: 'lblSumaResta', hdId: 'hdOpSR' },
        { labelId: 'lblMult', hdId: 'hdOpMult' },
        { labelId: 'lblDiv', hdId: 'hdOpDiv' }
    ];

    opConfig.forEach(function (cfg) {
        var lbl = document.getElementById(cfg.labelId);
        var hd = document.getElementById(cfg.hdId);
        if (!lbl || !hd) return;

        // Estado inicial visual
        if (hd.value === 'true') lbl.classList.add('op-check--active');

        lbl.addEventListener('click', function () {
            var activos = opConfig.filter(function (c) {
                return document.getElementById(c.hdId)?.value === 'true';
            });

            if (hd.value === 'true') {
                // Desmarcar solo si quedan más de 1 activo
                if (activos.length > 1) {
                    hd.value = 'false';
                    lbl.classList.remove('op-check--active');
                }
            } else {
                hd.value = 'true';
                lbl.classList.add('op-check--active');
            }
        });
    });

    // Estado inicial
    actualizarHints();

})();