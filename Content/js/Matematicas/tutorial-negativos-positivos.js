/* ============================================================
   tutorial-negativos-positivos.js
   Ruta: Content/js/Matematicas/tutorial-negativos-positivos.js
   Se carga DESPUÉS de ejercicio-negativos-positivos.js
   ============================================================ */
(function () {
    'use strict';

    /* ── Pasos del tutorial ──────────────────────────────────
       Cada paso define:
       - targetId   : ID del elemento a resaltar (null = sin spotlight)
       - titulo     : Título de la tarjeta
       - desc       : Descripción explicativa
       - emoji      : Emoji del astronauta/ícono
       - cardPos    : Posición de la tarjeta ('top','bottom','left','right','center')
       - arrowDir   : Dirección de la flecha ('top','bottom','left','right','none')
       - bloquear   : Si true, el usuario no puede interactuar con la pantalla
       - esPasoLibre: El overlay desaparece y el usuario resuelve el ejercicio
    ──────────────────────────────────────────────────────── */
    var PASOS = [
        {
            targetId: null,
            titulo: '¡Bienvenido al tutorial! 🚀',
            desc: 'En este tutorial aprenderás a usar el módulo de Números positivos y negativos paso a paso. ¡Es muy sencillo!',
            emoji: '🧑‍🚀',
            cardPos: 'center',
            arrowDir: 'none',
            bloquear: true,
            esPasoLibre: false
        },
        {
            targetId: 'timerWrap',
            titulo: 'Modo libre ♾️',
            desc: 'Este indicador muestra el tiempo. En modo libre no hay límite — puedes tomarte el tiempo que necesites para pensar.',
            emoji: '⏱',
            cardPos: 'bottom',
            arrowDir: 'top',
            bloquear: true,
            esPasoLibre: false
        },
        {
            targetId: 'operacionWrap',
            titulo: 'El ejercicio 🔢',
            desc: 'Aquí aparece la operación que debes resolver. El signo de interrogación "?" es el valor que tienes que encontrar.',
            emoji: '🔢',
            cardPos: 'bottom',
            arrowDir: 'top',
            bloquear: true,
            esPasoLibre: false
        },
        {
            targetId: 'rectaWrap',
            titulo: 'La recta numérica 📏',
            desc: 'Usa esta recta para seleccionar tu respuesta. Haz clic en el número que quieras, o usa los botones ‹ y › para moverte de uno en uno.',
            emoji: '📏',
            cardPos: 'top',
            arrowDir: 'bottom',
            bloquear: true,
            esPasoLibre: false
        },
        {
            targetId: 'rectaIndicador',
            titulo: 'Colores de ayuda 🎨',
            desc: 'El punto cambia de color según qué tan cerca estás:\n🔵 Azul = lejos\n🟡 Amarillo = ¡casi!\n🟢 Verde = ¡correcto!',
            emoji: '🎨',
            cardPos: 'top',
            arrowDir: 'bottom',
            bloquear: true,
            esPasoLibre: false
        },
        {
            targetId: 'btnTerminar',
            titulo: 'Salir del tutorial 🚪',
            desc: 'Si en cualquier momento presionas este botón, saldrás del tutorial y regresarás al mapa de aprendizaje.',
            emoji: '🚪',
            cardPos: 'bottom',
            arrowDir: 'top',
            bloquear: true,
            esPasoLibre: false
        },
        {
            targetId: null,
            titulo: '¡Ahora tú! 💪',
            desc: 'Mueve la recta hasta encontrar la respuesta correcta y luego presiona "Confirmar". ¡Tú puedes!',
            emoji: '💪',
            cardPos: 'bottom-right',
            arrowDir: 'none',
            bloquear: false,
            esPasoLibre: true
        }
    ];

    var TOTAL_PASOS = PASOS.length;
    var pasoActual = 0;

    /* ── Referencias DOM ─────────────────────────────────── */
    var overlay = document.getElementById('tutorialOverlay');
    var spotlight = document.getElementById('tutorialSpotlight');
    var card = document.getElementById('tutorialCard');
    var elPaso = document.getElementById('tutorialPaso');
    var elEmoji = document.getElementById('tutorialEmoji');
    var elTitulo = document.getElementById('tutorialTitulo');
    var elDesc = document.getElementById('tutorialDesc');
    var btnNext = document.getElementById('btnNextTutorial');
    var btnSkip = document.getElementById('btnSkipTutorial');
    var btnConfirmar = document.getElementById('btnConfirmar');
    var formFinalizar = document.getElementById('formFinalizar');
    var layout = document.getElementById('ejercicioLayout');

    if (!overlay) return; // No es modo tutorial, salir

    /* ── Agregar badge "TUTORIAL" al topbar ──────────────── */
    var topbar = document.getElementById('ejercicioTopbar');
    if (topbar) {
        var badge = document.createElement('span');
        badge.className = 'tutorial-badge';
        badge.innerHTML = '📚 Tutorial';
        topbar.insertBefore(badge, topbar.firstChild);
    }

    /* ── Barra de progreso ───────────────────────────────── */
    function crearBarraProgreso() {
        var barra = document.createElement('div');
        barra.className = 'tutorial-progress';
        barra.id = 'tutorialProgress';
        for (var i = 0; i < TOTAL_PASOS; i++) {
            var dot = document.createElement('div');
            dot.className = 'tutorial-progress__dot';
            dot.id = 'dot-' + i;
            barra.appendChild(dot);
        }
        // Insertar antes del título
        card.insertBefore(barra, elTitulo);
    }
    crearBarraProgreso();

    function actualizarBarraProgreso() {
        for (var i = 0; i < TOTAL_PASOS; i++) {
            var dot = document.getElementById('dot-' + i);
            if (dot) dot.classList.toggle('active', i <= pasoActual);
        }
    }

    /* ── Calcular posición del spotlight ─────────────────── */
    function posicionarSpotlight(targetId) {
        if (!targetId) {
            spotlight.style.width = '0px';
            spotlight.style.height = '0px';
            spotlight.style.top = '50%';
            spotlight.style.left = '50%';
            spotlight.classList.add('no-spotlight');
            return;
        }

        spotlight.classList.remove('no-spotlight');
        var el = document.getElementById(targetId);
        if (!el) { posicionarSpotlight(null); return; }

        var rect = el.getBoundingClientRect();
        var padding = 12;

        spotlight.style.top = (rect.top - padding) + 'px';
        spotlight.style.left = (rect.left - padding) + 'px';
        spotlight.style.width = (rect.width + padding * 2) + 'px';
        spotlight.style.height = (rect.height + padding * 2) + 'px';
    }

    /* ── Calcular posición de la tarjeta ─────────────────── */
    function posicionarCard(targetId, cardPos, arrowDir) {
        // Limpiar flechas anteriores
        card.classList.remove('arrow-top', 'arrow-bottom', 'arrow-left', 'arrow-right', 'arrow-none');
        card.classList.add('arrow-' + arrowDir);

        var W = card.offsetWidth || 300;
        var H = card.offsetHeight || 200;
        var vw = window.innerWidth;
        var vh = window.innerHeight;
        var margin = 20;

        if (cardPos === 'center') {
            card.style.top = ((vh - H) / 2) + 'px';
            card.style.left = ((vw - W) / 2) + 'px';
            return;
        }

        if (cardPos === 'bottom-right') {
            card.style.top = (vh - H - 100) + 'px';
            card.style.left = (vw - W - margin - 60) + 'px';
            return;
        }

        if (!targetId) {
            card.style.top = ((vh - H) / 2) + 'px';
            card.style.left = ((vw - W) / 2) + 'px';
            return;
        }

        var el = document.getElementById(targetId);
        if (!el) { posicionarCard(null, 'center', 'none'); return; }
        var rect = el.getBoundingClientRect();
        var padding = 12;

        var top, left;

        switch (cardPos) {
            case 'bottom':
                top = rect.bottom + padding + margin;
                left = rect.left + rect.width / 2 - W / 2;
                break;
            case 'top':
                top = rect.top - padding - margin - H;
                left = rect.left + rect.width / 2 - W / 2;
                break;
            case 'right':
                top = rect.top + rect.height / 2 - H / 2;
                left = rect.right + padding + margin;
                break;
            case 'left':
                top = rect.top + rect.height / 2 - H / 2;
                left = rect.left - padding - margin - W;
                break;
            default:
                top = rect.bottom + margin;
                left = rect.left;
        }

        // Mantener dentro del viewport
        left = Math.max(margin, Math.min(vw - W - margin, left));
        top = Math.max(margin, Math.min(vh - H - margin, top));

        card.style.top = top + 'px';
        card.style.left = left + 'px';
    }

    /* ── Mostrar paso ─────────────────────────────────────── */
    function mostrarPaso(idx) {
        if (idx >= TOTAL_PASOS) {
            completarTutorial();
            return;
        }

        var paso = PASOS[idx];
        pasoActual = idx;

        // Actualizar contenido
        elPaso.textContent = 'Paso ' + (idx + 1) + ' de ' + TOTAL_PASOS;
        elEmoji.textContent = paso.emoji;
        elTitulo.textContent = paso.titulo;
        elDesc.textContent = paso.desc;

        actualizarBarraProgreso();

        // Spotlight
        posicionarSpotlight(paso.targetId);

        // Paso libre: el usuario resuelve el ejercicio
        if (paso.esPasoLibre) {
            overlay.classList.add('paso-libre');
            overlay.style.pointerEvents = 'none';
            card.style.pointerEvents = 'all';

            // Cambiar botón a "Completar tutorial"
            btnNext.textContent = '¡Listo! Completar tutorial';
            btnNext.className = 'tutorial-btn tutorial-btn--finish';
            btnSkip.style.display = 'none';

            // Posicionar la card en esquina inferior
            posicionarCard(null, 'bottom-right', 'none');

            // Cuando el usuario confirme el ejercicio, completar el tutorial
            if (btnConfirmar) {
                btnConfirmar.addEventListener('click', function onConfirm() {
                    btnConfirmar.removeEventListener('click', onConfirm);
                    setTimeout(completarTutorial, 800);
                }, { once: true });
            }
        } else {
            overlay.classList.remove('paso-libre');
            overlay.style.pointerEvents = paso.bloquear ? 'all' : 'none';
            btnNext.textContent = idx === TOTAL_PASOS - 2 ? '¡Entendido! Practicar →' : 'Entendido →';
            btnNext.className = 'tutorial-btn tutorial-btn--next';
            btnSkip.style.display = 'inline-block';

            // Posicionar card con delay para que el spotlight animate primero
            setTimeout(function () {
                posicionarCard(paso.targetId, paso.cardPos, paso.arrowDir);
            }, 50);
        }

        // Re-animar la card
        card.style.animation = 'none';
        card.offsetHeight; // reflow
        card.style.animation = '';
    }

    /* ── Completar tutorial ───────────────────────────────── */
    function completarTutorial() {
        // Enviar el form de finalizar (que en el controller redirige al dashboard)
        if (formFinalizar) formFinalizar.submit();
    }

    /* ── Eventos de botones ───────────────────────────────── */
    if (btnNext) {
        btnNext.addEventListener('click', function () {
            mostrarPaso(pasoActual + 1);
        });
    }

    if (btnSkip) {
        btnSkip.addEventListener('click', function () {
            if (confirm('¿Seguro que quieres saltar el tutorial?')) {
                completarTutorial();
            }
        });
    }

    // Recalcular posiciones al redimensionar
    window.addEventListener('resize', function () {
        var paso = PASOS[pasoActual];
        posicionarSpotlight(paso.targetId);
        posicionarCard(paso.targetId, paso.cardPos, paso.arrowDir);
    });

    /* ── Arrancar en el paso 0 ───────────────────────────── */
    // Pequeño delay para que los elementos del DOM estén pintados
    setTimeout(function () {
        mostrarPaso(0);
    }, 300);

})();