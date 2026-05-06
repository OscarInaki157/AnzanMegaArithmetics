/* ============================================================
   ejercicio-negativos-positivos.js
   Ruta: Content/js/Matematicas/ejercicio-negativos-positivos.js
   ============================================================ */
(function () {
    'use strict';

    var layout = document.getElementById('ejercicioLayout');
    var formAvanzar = document.getElementById('formAvanzar');
    var formFinalizar = document.getElementById('formFinalizar');
    var hdRespuesta = document.getElementById('hdRespuesta');
    var hdTiempo = document.getElementById('hdTiempo');
    var btnConfirmar = document.getElementById('btnConfirmar');
    var btnTerminar = document.getElementById('btnTerminar');
    var resultadoEl = document.getElementById('resultadoDisplay');
    var opDisplay = document.getElementById('operacionDisplay');

    var velocidad = layout ? layout.dataset.velocidad : '0';
    var respCorrecta = layout ? parseInt(layout.dataset.respuestaCorrecta, 10) : 0;
    var usaRecta = layout ? layout.dataset.usaRecta === 'true' : false;

    var valorSeleccionado = null;
    var tiempoTranscurrido = 0;
    var timerInterval = null;
    var TICK = 100;

    /* ── Feedback visual compartido ──────────────────────── */
    function feedbackClase(val) {
        if (val === null) return 'lejos';
        var diff = Math.abs(val - respCorrecta);
        if (diff === 0) return 'correcto';
        if (diff <= 3) return 'cerca';
        return 'lejos';
    }

    function actualizarResultadoDisplay(val) {
        if (!resultadoEl) return;
        resultadoEl.textContent = (val === null) ? '?' : val;
        resultadoEl.className = 'op-num op-incognita ' + feedbackClase(val);
    }

    /* ══════════════════════════════════════════════════════
       RECTA NUMÉRICA
    ══════════════════════════════════════════════════════ */
    if (usaRecta) {
        var recta = document.getElementById('recta');
        var indicador = document.getElementById('rectaIndicador');
        var indicadorVal = document.getElementById('indicadorVal');
        var btnIzq = document.getElementById('btnIzq');
        var btnDer = document.getElementById('btnDer');
        var MIN = -20, MAX = 20, RANGO = 40;

        function posicionarMarcas() {
            document.querySelectorAll('.recta-marca').forEach(function (m) {
                m.style.left = ((parseInt(m.dataset.val) - MIN) / RANGO * 100) + '%';
            });
        }
        posicionarMarcas();
        window.addEventListener('resize', posicionarMarcas);

        function moverIndicador(val) {
            indicador.style.left = ((val - MIN) / RANGO * 100) + '%';
            indicadorVal.textContent = val;
        }
        moverIndicador(0);

        function seleccionar(val) {
            val = Math.max(MIN, Math.min(MAX, val));
            valorSeleccionado = val;
            moverIndicador(val);

            var clase = feedbackClase(val);
            indicador.classList.remove('recta-indicador--cerca', 'recta-indicador--correcto');
            if (clase === 'cerca') indicador.classList.add('recta-indicador--cerca');
            if (clase === 'correcto') indicador.classList.add('recta-indicador--correcto');

            actualizarResultadoDisplay(val);
            if (btnConfirmar) btnConfirmar.disabled = false;
        }

        recta.addEventListener('click', function (e) {
            var r = recta.getBoundingClientRect();
            var ratio = Math.max(0, Math.min(1, (e.clientX - r.left) / r.width));
            seleccionar(Math.round(MIN + ratio * RANGO));
        });
        btnIzq.addEventListener('click', function () { seleccionar((valorSeleccionado || 0) - 1); });
        btnDer.addEventListener('click', function () { seleccionar((valorSeleccionado || 0) + 1); });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowLeft') { e.preventDefault(); seleccionar((valorSeleccionado || 0) - 1); }
            if (e.key === 'ArrowRight') { e.preventDefault(); seleccionar((valorSeleccionado || 0) + 1); }
            if (e.key === 'Enter' && btnConfirmar && !btnConfirmar.disabled) confirmar();
        });
    }

    /* ══════════════════════════════════════════════════════
       TECLADO NUMÉRICO
    ══════════════════════════════════════════════════════ */
    if (!usaRecta) {
        var tecladoDisplay = document.getElementById('tecladoDisplay');
        var tecladoVal = document.getElementById('tecladoVal');
        var btnNeg = document.getElementById('btnNeg');
        var btnDel = document.getElementById('btnDel');
        var strValor = '';   // string del número en construcción
        var esNegativo = false;

        function renderTeclado() {
            if (strValor === '') {
                tecladoVal.textContent = '—';
                tecladoDisplay.className = 'teclado-display vacio';
                valorSeleccionado = null;
                if (btnConfirmar) btnConfirmar.disabled = true;
                return;
            }

            var num = parseInt((esNegativo ? '-' : '') + strValor, 10);
            valorSeleccionado = num;

            tecladoVal.textContent = (esNegativo ? '-' : '') + strValor;

            var clase = feedbackClase(num);
            tecladoDisplay.className = 'teclado-display ' + clase;

            actualizarResultadoDisplay(num);
            if (btnConfirmar) btnConfirmar.disabled = false;
        }

        // Botones de dígitos
        document.querySelectorAll('.teclado-btn[data-digit]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                if (strValor.length >= 6) return;  // límite de dígitos
                strValor += btn.dataset.digit;
                renderTeclado();
            });
        });

        // Signo ±
        if (btnNeg) {
            btnNeg.addEventListener('click', function () {
                esNegativo = !esNegativo;
                btnNeg.style.color = esNegativo ? '#dc2626' : '#6677cc';
                renderTeclado();
            });
        }

        // Borrar
        if (btnDel) {
            btnDel.addEventListener('click', function () {
                strValor = strValor.slice(0, -1);
                if (strValor === '') esNegativo = false;
                renderTeclado();
            });
        }

        // Teclado físico
        document.addEventListener('keydown', function (e) {
            if (e.key >= '0' && e.key <= '9') {
                if (strValor.length < 6) { strValor += e.key; renderTeclado(); }
            } else if (e.key === 'Backspace') {
                strValor = strValor.slice(0, -1);
                if (strValor === '') esNegativo = false;
                renderTeclado();
            } else if (e.key === '-') {
                esNegativo = !esNegativo;
                renderTeclado();
            } else if (e.key === 'Enter' && btnConfirmar && !btnConfirmar.disabled) {
                confirmar();
            }
        });
    }

    /* ══════════════════════════════════════════════════════
       TIMER
    ══════════════════════════════════════════════════════ */
    var timerNumEl = document.getElementById('timerNum');
    var tiempoRestante = parseFloat(velocidad) || 0;

    if (timerNumEl && velocidad !== '0') {
        timerInterval = setInterval(function () {
            tiempoTranscurrido += TICK / 1000;
            tiempoRestante -= TICK / 1000;
            timerNumEl.textContent = Math.max(0, tiempoRestante).toFixed(0);
            if (tiempoRestante <= 5) timerNumEl.classList.add('urgente');
            if (tiempoRestante <= 0) { clearInterval(timerInterval); enviarRespuesta(); }
        }, TICK);
    }

    /* ══════════════════════════════════════════════════════
       CONFIRMAR
    ══════════════════════════════════════════════════════ */
    if (btnConfirmar) btnConfirmar.addEventListener('click', confirmar);

    function confirmar() {
        if (valorSeleccionado === null) return;
        clearInterval(timerInterval);

        var esCorrecto = valorSeleccionado === respCorrecta;
        if (opDisplay) {
            opDisplay.classList.add(esCorrecto ? 'flash-correct' : 'flash-wrong');
            setTimeout(function () {
                opDisplay.classList.remove('flash-correct', 'flash-wrong');
            }, 600);
        }
        setTimeout(enviarRespuesta, esCorrecto ? 400 : 650);
    }

    function enviarRespuesta() {
        if (!formAvanzar) return;
        hdRespuesta.value = valorSeleccionado !== null ? valorSeleccionado : '';
        hdTiempo.value = tiempoTranscurrido.toFixed(2);
        formAvanzar.submit();
    }

    /* ══════════════════════════════════════════════════════
       TERMINAR
    ══════════════════════════════════════════════════════ */
    if (btnTerminar && formFinalizar) {
        btnTerminar.addEventListener('click', function () {
            clearInterval(timerInterval);
            formFinalizar.submit();
        });
    }

})();