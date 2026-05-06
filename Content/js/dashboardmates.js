/* ============================================================
   dashboardmates.js — MatesMéxico
   ============================================================ */

document.addEventListener('DOMContentLoaded', function () {

    /* ── 1. Hamburger ──────────────────────────────────────── */
    var hamburger = document.getElementById('mm-hamburger');
    var sidebar = document.querySelector('.mm-sidebar');
    if (hamburger && sidebar) {
        hamburger.addEventListener('click', function (e) {
            e.stopPropagation();
            sidebar.classList.toggle('open');
        });
        document.addEventListener('click', function (e) {
            if (sidebar.classList.contains('open') && !sidebar.contains(e.target))
                sidebar.classList.remove('open');
        });
    }

    /* ── 2. Tooltip cards ──────────────────────────────────── */
    var openTip = null;

    document.getElementById('pathScroll') &&
        document.getElementById('pathScroll').addEventListener('click', function (e) {
            var wrap = e.target.closest('.hex-node, .chest-node, .special-node');
            if (!wrap) { closeAllTips(); return; }
            e.stopPropagation();
            var tip = wrap.querySelector('.node-tip');
            if (!tip) return;
            if (tip === openTip) { closeTip(tip); return; }
            closeAllTips();
            openTip = tip;
            tip.classList.add('open');
        });

    document.addEventListener('click', closeAllTips);
    document.querySelectorAll('.node-tip').forEach(function (t) {
        t.addEventListener('click', function (e) { e.stopPropagation(); });
    });

    function closeTip(t) { t.classList.remove('open'); if (openTip === t) openTip = null; }
    function closeAllTips() {
        document.querySelectorAll('.node-tip.open').forEach(function (t) { t.classList.remove('open'); });
        openTip = null;
    }

    /* ── 3. Scroll-to-top ──────────────────────────────────── */
    var btn = document.getElementById('scrollTopBtn');
    var area = document.getElementById('pathScroll');
    if (btn && area) {
        area.addEventListener('scroll', function () {
            btn.classList.toggle('visible', area.scrollTop > 200);
        });
        btn.addEventListener('click', function () {
            area.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    /* ── 4. XP counter ─────────────────────────────────────── */
    var xpEl = document.getElementById('xp-counter-val');
    if (xpEl) {
        var target = parseInt(xpEl.dataset.xp, 10) || 0;
        var frames = Math.round(1.8 * 60), cur = 0;
        var iv = setInterval(function () {
            cur++;
            var p = cur / frames;
            var e = p === 1 ? 1 : 1 - Math.pow(2, -10 * p);
            xpEl.textContent = Math.round(target * e).toLocaleString('es-MX') + ' XP';
            if (cur >= frames) { clearInterval(iv); xpEl.textContent = target.toLocaleString('es-MX') + ' XP'; }
        }, 1000 / 60);
    }

    /* ── 5. Progress bars ──────────────────────────────────── */
    setTimeout(function () {
        document.querySelectorAll('.ub-bar-fill[data-width]').forEach(function (b) {
            b.style.width = b.dataset.width + '%';
        });
    }, 400);

    /* ── 6. Shimmer nodo personaje ─────────────────────────── */
    var charBox = document.querySelector('.special-box.char-unlock');
    if (charBox) {
        setInterval(function () {
            charBox.style.boxShadow = '0 0 18px rgba(167,139,250,.7)';
            setTimeout(function () { charBox.style.boxShadow = '0 4px 0 #5b21b6'; }, 500);
        }, 2400);
    }

    /* ══════════════════════════════════════════════════════════
       7. CONECTORES DINÁMICOS
          Solo conecta nodos del camino (.hex-node, .chest-node
          primer/último del grupo, .special-node).
          El astronauta (.astro-wrap) se excluye de la cadena
          de conexión pero los conectores sí pasan cerca de él
          de forma natural por la curva Bezier.
    ══════════════════════════════════════════════════════════ */

    var UNIT_COLORS = {
        'u-indigo': '#6d28d9',
        'u-green': '#16a34a',
        'u-amber': '#d97706',
        'u-coming': '#a5b4fc'
    };

    function getColor(hexPath) {
        var el = hexPath.previousElementSibling;
        while (el) {
            for (var c in UNIT_COLORS)
                if (el.classList && el.classList.contains(c)) return UNIT_COLORS[c];
            el = el.previousElementSibling;
        }
        return '#94a3b8';
    }

    /* Elemento visual clickable dentro de un nodo */
    function visual(node) {
        return node.querySelector('.hex-shape, .chest-box, .special-box') || node;
    }

    /* Rectángulo relativo al hex-path */
    function rel(el, base) {
        var er = el.getBoundingClientRect();
        var br = base.getBoundingClientRect();
        return {
            top: er.top - br.top,
            bottom: er.bottom - br.top,
            left: er.left - br.left,
            right: er.right - br.left,
            cx: (er.left - br.left) + er.width / 2,
            cy: (er.top - br.top) + er.height / 2
        };
    }

    /* Dibuja curva cúbica entre borde inferior de A y borde superior de B */
    function curve(svg, ax, ay, bx, by, color) {
        var dy = by - ay;
        var dx = bx - ax;
        var cpv = Math.max(Math.abs(dy) * 0.45, 24);
        var cph = Math.min(Math.abs(dx) * 0.3, 80);
        var sx = dx >= 0 ? 1 : -1;

        var d = ['M', ax, ay,
            'C', ax + sx * cph, ay + cpv,
            bx - sx * cph, by - cpv,
            bx, by].join(' ');

        var p = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        p.setAttribute('d', d);
        p.setAttribute('fill', 'none');
        p.setAttribute('stroke', color);
        p.setAttribute('stroke-width', '3');
        p.setAttribute('stroke-dasharray', '8 5');
        p.setAttribute('stroke-linecap', 'round');
        svg.appendChild(p);
    }

    function buildConnectors() {
        /* Limpiar conectores previos (estáticos o generados) */
        document.querySelectorAll('.path-connector, .mm-connector-svg').forEach(function (el) {
            el.remove();
        });

        document.querySelectorAll('.hex-path').forEach(function (hp) {
            hp.style.position = 'relative';

            var color = getColor(hp);

            /* ── Construir lista ordenada de "puntos de parada" ── */
            /* Reglas:
               - .hex-node        → 1 punto (el propio nodo)
               - .chest-group     → 2 puntos: primer y último cofre del grupo
               - .special-node    → 1 punto
               - .astro-wrap      → IGNORADO (decoración, no es parada)
            */
            var stops = [];

            hp.childNodes.forEach(function (child) {
                if (child.nodeType !== 1) return; // solo elementos

                if (child.classList.contains('hex-row')) {
                    /* Dentro de hex-row puede haber hex-node, chest-group, special-node */
                    var hexNodes = child.querySelectorAll('.hex-node');
                    hexNodes.forEach(function (n) { stops.push(n); });

                    var groups = child.querySelectorAll('.chest-group');
                    groups.forEach(function (g) {
                        var chests = g.querySelectorAll('.chest-node');
                        if (chests.length > 0) stops.push(chests[0]);      // primero
                        if (chests.length > 1) stops.push(chests[chests.length - 1]); // último
                    });

                    var specials = child.querySelectorAll('.special-node');
                    specials.forEach(function (s) { stops.push(s); });

                    /* Cofres directamente en hex-row sin chest-group */
                    if (groups.length === 0) {
                        var directChests = child.querySelectorAll('.chest-node');
                        directChests.forEach(function (c) { stops.push(c); });
                    }
                }

                /* chest-group directo hijo de hex-path (sin hex-row) */
                if (child.classList.contains('chest-group')) {
                    var chests = child.querySelectorAll('.chest-node');
                    if (chests.length > 0) stops.push(chests[0]);
                    if (chests.length > 1) stops.push(chests[chests.length - 1]);
                }

                /* hex-node / special-node / chest-node directo */
                if (child.classList.contains('hex-node') ||
                    child.classList.contains('special-node') ||
                    child.classList.contains('chest-node')) {
                    stops.push(child);
                }
                /* astro-wrap → omitido intencionalmente */
            });

            if (stops.length < 2) return;

            /* ── Crear SVG de fondo ── */
            var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            svg.setAttribute('class', 'mm-connector-svg');
            svg.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;overflow:visible;z-index:1;';
            hp.appendChild(svg);

            var base = hp.getBoundingClientRect();

            /* ── Dibujar curva entre cada par consecutivo ── */
            for (var i = 0; i < stops.length - 1; i++) {
                var va = visual(stops[i]);
                var vb = visual(stops[i + 1]);

                var ra = va.getBoundingClientRect();
                var rb = vb.getBoundingClientRect();

                /* Salida: borde inferior centro de A */
                var ax = (ra.left - base.left) + ra.width / 2;
                var ay = (ra.bottom - base.top);

                /* Entrada: borde superior centro de B */
                var bx = (rb.left - base.left) + rb.width / 2;
                var by = (rb.top - base.top);

                curve(svg, ax, ay, bx, by, color);
            }
        });
    }

    /* Esperar que el layout esté pintado antes de calcular posiciones */
    requestAnimationFrame(function () {
        requestAnimationFrame(buildConnectors);
    });

    /* Re-dibujar al cambiar tamaño */
    var rt;
    window.addEventListener('resize', function () {
        clearTimeout(rt);
        rt = setTimeout(function () {
            document.querySelectorAll('.mm-connector-svg').forEach(function (s) { s.remove(); });
            buildConnectors();
        }, 160);
    });

});
