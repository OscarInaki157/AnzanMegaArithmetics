/* ejercicio-leyes-signos-operaciones.js */
(function () {
    'use strict';

    var layout = document.getElementById('ejercicioLayout');
    if (!layout) return;

    var formAvanzar = document.getElementById('formAvanzar');
    var formFinalizar = document.getElementById('formFinalizar');
    var hdRespuesta = document.getElementById('hdRespuesta');
    var hdTiempo = document.getElementById('hdTiempo');
    var btnConfirmar = document.getElementById('btnConfirmar');
    var btnTerminar = document.getElementById('btnTerminar');
    var resultadoEl = document.getElementById('resultadoDisplay');

    var velocidad = layout.dataset.velocidad;
    var respCorrecta = parseFloat(layout.dataset.respuestaCorrecta);
    var coefRespuesta = parseInt(layout.dataset.coefRespuesta, 10);
    var esPuroSigno = layout.dataset.esPuroSigno === 'true';
    var esLiteral = layout.dataset.esLiteral === 'true';
    var usaDecimales = layout.dataset.usaDecimales === 'true';

    var valorSeleccionado = null;   // número o string "+" / "-"
    var tiempoTranscurrido = 0;
    var timerInterval = null;
    var bloqueado = false;
    var TICK = 100;

    /* ══════════════════════════════════════════════════════
       PURO SIGNO — botones + / −
    ══════════════════════════════════════════════════════ */
    if (esPuroSigno) {
        var btnPos = document.getElementById('btnSignoPos');
        var btnNeg = document.getElementById('btnSignoNeg');
        var incogEl = document.querySelector('.signo-grande--incognita');

        function seleccionarSigno(val) {
            if (bloqueado) return;
            valorSeleccionado = val;

            if (incogEl) {
                incogEl.textContent = val === '+' ? '+' : '−';
                incogEl.classList.remove('seleccionado-pos', 'seleccionado-neg');
                incogEl.classList.add(val === '+' ? 'seleccionado-pos' : 'seleccionado-neg');
            }
            [btnPos, btnNeg].forEach(function (b) { if (b) b.classList.remove('seleccionado'); });
            var sel = val === '+' ? btnPos : btnNeg;
            if (sel) sel.classList.add('seleccionado');

            if (btnConfirmar) btnConfirmar.disabled = false;
        }

        if (btnPos) btnPos.addEventListener('click', function () { seleccionarSigno('+'); });
        if (btnNeg) btnNeg.addEventListener('click', function () { seleccionarSigno('-'); });

        document.addEventListener('keydown', function (e) {
            if (bloqueado) return;
            if (e.key === '+') seleccionarSigno('+');
            if (e.key === '-') seleccionarSigno('-');
            if (e.key === 'Enter' && btnConfirmar && !btnConfirmar.disabled) confirmar();
        });
    }

    /* ══════════════════════════════════════════════════════
       TECLADO NUMÉRICO (Literales, Reales, Paréntesis, Exponente)
    ══════════════════════════════════════════════════════ */
    if (!esPuroSigno) {
        var tecDisplay = document.getElementById('tecladoDisplay');
        var tecVal = document.getElementById('tecladoVal');
        var varSuffix = document.getElementById('varRespuesta'); // en literales muestra la variable
        var btnNeg_ = document.getElementById('btnNeg');
        var btnDel = document.getElementById('btnDel');
        var btnDec = document.getElementById('btnDec');
        var strValor = '';
        var esNegativo = false;
        var tieneDecimal = false;

        function renderTeclado() {
            if (strValor === '') {
                if (tecVal) tecVal.textContent = '—';
                if (tecDisplay) tecDisplay.className = 'teclado-display vacio';
                if (resultadoEl) { resultadoEl.textContent = '?'; resultadoEl.className = 'op-resultado lejos'; }
                if (varSuffix) varSuffix.className = 'op-var-resp';
                valorSeleccionado = null;
                if (btnConfirmar) btnConfirmar.disabled = true;
                return;
            }

            var displayStr = (esNegativo ? '−' : '') + strValor;
            if (tecVal) tecVal.textContent = displayStr;

            var num = parseFloat((esNegativo ? '-' : '') + strValor);
            valorSeleccionado = isNaN(num) ? null : (esLiteral ? Math.round(num) : num);

            if (valorSeleccionado !== null) {
                actualizarFeedback(valorSeleccionado);
                if (btnConfirmar) btnConfirmar.disabled = false;
            }
        }

        function actualizarFeedback(val) {
            var correcto = esLiteral
                ? val === coefRespuesta
                : Math.abs(val - respCorrecta) < 0.01;
            var cerca = esLiteral
                ? Math.abs(val - coefRespuesta) <= 2
                : Math.abs(val - respCorrecta) <= Math.max(3, Math.abs(respCorrecta) * 0.15);

            var clase = correcto ? 'correcto' : cerca ? 'cerca' : 'lejos';

            if (resultadoEl) {
                resultadoEl.textContent = val;
                resultadoEl.className = 'op-resultado ' + clase;
            }
            if (tecDisplay) tecDisplay.className = 'teclado-display ' + clase;

            // En literales, colorear también el sufijo de variable
            if (varSuffix) varSuffix.className = 'op-var-resp ' + clase;
        }

        document.querySelectorAll('.teclado-btn[data-digit]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                if (bloqueado || strValor.length >= 8) return;
                strValor += btn.dataset.digit;
                renderTeclado();
            });
        });

        if (btnNeg_) {
            btnNeg_.addEventListener('click', function () {
                if (bloqueado) return;
                esNegativo = !esNegativo;
                btnNeg_.style.color = esNegativo ? '#dc2626' : '#6677cc';
                renderTeclado();
            });
        }
        if (btnDec) {
            btnDec.addEventListener('click', function () {
                if (bloqueado || tieneDecimal) return;
                if (strValor === '') strValor = '0';
                tieneDecimal = true; strValor += '.';
                renderTeclado();
            });
        }
        if (btnDel) {
            btnDel.addEventListener('click', function () {
                if (bloqueado) return;
                if (strValor.slice(-1) === '.') tieneDecimal = false;
                strValor = strValor.slice(0, -1);
                if (strValor === '') esNegativo = false;
                renderTeclado();
            });
        }

        document.addEventListener('keydown', function (e) {
            if (bloqueado) return;
            if (e.key >= '0' && e.key <= '9') {
                if (strValor.length < 8) { strValor += e.key; renderTeclado(); }
            } else if (e.key === '.' && !tieneDecimal && usaDecimales) {
                if (strValor === '') strValor = '0';
                tieneDecimal = true; strValor += '.'; renderTeclado();
            } else if (e.key === 'Backspace') {
                if (strValor.slice(-1) === '.') tieneDecimal = false;
                strValor = strValor.slice(0, -1);
                if (strValor === '') esNegativo = false;
                renderTeclado();
            } else if (e.key === '-') {
                esNegativo = !esNegativo; renderTeclado();
            } else if (e.key === 'Enter' && btnConfirmar && !btnConfirmar.disabled) {
                confirmar();
            }
        });
    }

    if (btnConfirmar) btnConfirmar.addEventListener('click', confirmar);

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
            if (tiempoRestante <= 0) {
                clearInterval(timerInterval);
                enviarRespuesta('omitido');
            }
        }, TICK);
    }

    /* ══════════════════════════════════════════════════════
       CONFIRMAR — un solo clic, envío directo
    ══════════════════════════════════════════════════════ */
    function confirmar() {
        if (bloqueado) return;
        if (valorSeleccionado === null) return;
        bloqueado = true;
        clearInterval(timerInterval);

        // Bloquear inputs
        if (btnConfirmar) btnConfirmar.disabled = true;
        document.querySelectorAll('.teclado-btn, .signo-btn').forEach(function (b) {
            b.disabled = true;
        });

        // Determinar si es correcto
        var esCorrecto;
        if (esPuroSigno) {
            esCorrecto = (valorSeleccionado === '+') === (respCorrecta >= 0);
        } else if (esLiteral) {
            esCorrecto = Math.round(Number(valorSeleccionado)) === coefRespuesta;
        } else {
            esCorrecto = Math.abs(Number(valorSeleccionado) - respCorrecta) < 0.01;
        }

        // Feedback visual en el display del ejercicio
        var display = document.getElementById('ejercicioDisplay');
        if (display) {
            display.classList.add(esCorrecto ? 'flash-correct' : 'flash-wrong');
            setTimeout(function () {
                display.classList.remove('flash-correct', 'flash-wrong');
            }, 700);
        }

        // Si falló: mostrar respuesta correcta
        if (!esCorrecto && resultadoEl) {
            setTimeout(function () {
                var corrStr;
                if (esPuroSigno) corrStr = respCorrecta >= 0 ? '+' : '−';
                else if (esLiteral) corrStr = String(coefRespuesta);
                else corrStr = respCorrecta % 1 === 0
                    ? String(respCorrecta)
                    : respCorrecta.toFixed(2);
                resultadoEl.textContent = corrStr;
                resultadoEl.className = 'op-resultado incorrecto';
            }, 300);
        }

        // Construir string de respuesta para el server
        var respStr;
        if (esPuroSigno) respStr = String(valorSeleccionado);
        else if (esLiteral) respStr = String(Math.round(Number(valorSeleccionado)));
        else respStr = String(valorSeleccionado).replace(',', '.');

        // Enviar directo — un solo clic
        setTimeout(function () {
            enviarRespuesta(respStr);
        }, esCorrecto ? 600 : 1200);
    }

    function enviarRespuesta(respStr) {
        if (!formAvanzar) return;
        hdRespuesta.value = respStr || 'omitido';
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

    /* API para tutorial */
    window.LeyesSignosOperacionesAPI = {
        confirmar: confirmar,
        getValorSeleccionado: function () { return valorSeleccionado; },
        getTiempoTranscurrido: function () { return tiempoTranscurrido; }
    };

})();
