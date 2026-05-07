using AnzanMegaArithmetics.Models.MatematicasModels.LeyesSignosOperaciones;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers.Matematicas
{
    public class LeyesSignosOperacionesController : Controller
    {
        private static readonly Random _rng = new();

        [HttpGet]
        public IActionResult Formulario() =>
            View("FormularioLeyesSignosOperaciones", new ConfLeyesSignosOperacionesViewModel());

        [HttpPost]
        public IActionResult Iniciar(ConfLeyesSignosOperacionesViewModel config)
        {
            if (!ModelState.IsValid)
                return View("FormularioLeyesSignosOperaciones", config);


            config.OpSumaResta = Request.Form["OpSumaResta"].ToString().ToLower() == "true";
            config.OpMultiplicacion = Request.Form["OpMultiplicacion"].ToString().ToLower() == "true";
            config.OpDivision = Request.Form["OpDivision"].ToString().ToLower() == "true";

            config.CantidadEjercicios = Math.Clamp(config.CantidadEjercicios, 1, 100);
            config.CantidadOperandos = Math.Clamp(config.CantidadOperandos, 2, 10);
            config.MinDigitos = Math.Clamp(config.MinDigitos, 1, 3);
            config.MaxDigitos = Math.Clamp(config.MaxDigitos, config.MinDigitos, 3);

            if (!config.OpSumaResta && !config.OpMultiplicacion && !config.OpDivision)
                config.OpSumaResta = true;

            if (config.TiposSeleccionados == null || config.TiposSeleccionados.Count == 0)
                config.TiposSeleccionados = new List<int> { 0 };

            var estado = new EjercicioLeyesSignosOperacionesViewModel
            {
                CantidadEjercicios = config.CantidadEjercicios,
                CantidadOperandos = config.CantidadOperandos,
                MinDigitos = config.MinDigitos,
                MaxDigitos = config.MaxDigitos,
                TiposSeleccionadosJson = JsonSerializer.Serialize(config.TiposSeleccionados),
                OpSumaResta = config.OpSumaResta,
                OpMultiplicacion = config.OpMultiplicacion,
                OpDivision = config.OpDivision,
                VelocidadEjercicio = config.VelocidadEjercicio,
                EjerciciosRealizados = 0,
                ResultadosJson = "[]",
                EsTutorial = false
            };

            GenerarEjercicio(estado);
            return View("EjercicioLeyesSignosOperaciones", estado);
        }

        [HttpGet]
        public IActionResult IniciarTutorial()
        {
            var estado = new EjercicioLeyesSignosOperacionesViewModel
            {
                CantidadEjercicios = 1,
                CantidadOperandos = 2,
                MinDigitos = 1,
                MaxDigitos = 1,
                TiposSeleccionadosJson = "[0]",
                OpSumaResta = true,
                OpMultiplicacion = true,
                OpDivision = true,
                VelocidadEjercicio = "0",
                EjerciciosRealizados = 0,
                ResultadosJson = "[]",
                EsTutorial = true
            };
            GenerarEjercicio(estado);
            return View("EjercicioLeyesSignosOperaciones", estado);
        }

        [HttpPost]
        public IActionResult Avanzar(EjercicioLeyesSignosOperacionesViewModel estado)
        {
            ModelState.Clear();
            var lista = Deserializar(estado.ResultadosJson);
            lista.Add(new ResultadoLeyesSignosOperaciones
            {
                TipoEjercicio = estado.TipoEjercicioActual,
                CantidadOperandos = estado.CantidadOperandos,
                RespuestaCorrecta = estado.RespuestaCorrecta,
                CoefRespuesta = estado.CoefRespuesta,
                RespuestaUsuario = estado.RespuestaUsuario ?? "omitido",
                TiempoRespuesta = estado.TiempoRespuesta,
                Variable = estado.Variable,
                Enunciado = ConstruirEnunciadoResultado(estado)
            });

            estado.EjerciciosRealizados++;
            estado.ResultadosJson = JsonSerializer.Serialize(lista);

            if (estado.EsTutorial)
                return RedirectToAction("DashboardMatematicas", "DashboardMatematicas");

            if (estado.EjerciciosRealizados >= estado.CantidadEjercicios)
                return VistaResultado(lista, estado);

            GenerarEjercicio(estado);
            return View("EjercicioLeyesSignosOperaciones", estado);
        }

        [HttpPost]
        public IActionResult Finalizar(EjercicioLeyesSignosOperacionesViewModel estado)
        {
            ModelState.Clear();
            if (estado.EsTutorial)
                return RedirectToAction("DashboardMatematicas", "DashboardMatematicas");

            var lista = Deserializar(estado.ResultadosJson);
            while (lista.Count < estado.CantidadEjercicios)
                lista.Add(new ResultadoLeyesSignosOperaciones { RespuestaUsuario = "omitido" });

            return VistaResultado(lista, estado);
        }

        [HttpGet]
        public IActionResult RegresarDashboard() =>
            RedirectToAction("DashboardMatematicas", "DashboardMatematicas");

        // ════════════════════════════════════════════════════
        // POOL DE OPERACIONES — respeta configuración
        // ════════════════════════════════════════════════════

        private List<OperacionLeyes> OpsPool(EjercicioLeyesSignosOperacionesViewModel e,
                                              bool soloSumaResta = false)
        {
            var pool = new List<OperacionLeyes>();

            if (soloSumaResta)
            {
                // Literales siempre usan solo suma/resta
                pool.Add(OperacionLeyes.Suma);
                pool.Add(OperacionLeyes.Resta);
                return pool;
            }

            if (e.OpSumaResta) { pool.Add(OperacionLeyes.Suma); pool.Add(OperacionLeyes.Resta); }
            if (e.OpMultiplicacion) { pool.Add(OperacionLeyes.Multiplicacion); }
            if (e.OpDivision) { pool.Add(OperacionLeyes.Division); }
            if (pool.Count == 0) { pool.Add(OperacionLeyes.Suma); pool.Add(OperacionLeyes.Resta); }

            return pool;
        }

        private OperacionLeyes ElegirOp(EjercicioLeyesSignosOperacionesViewModel e,
                                         bool soloSumaResta = false)
        {
            var pool = OpsPool(e, soloSumaResta);
            return pool[_rng.Next(pool.Count)];
        }

        // ════════════════════════════════════════════════════
        // GENERADORES
        // ════════════════════════════════════════════════════

        private void GenerarEjercicio(EjercicioLeyesSignosOperacionesViewModel e)
        {
            var tipos = JsonSerializer.Deserialize<List<int>>(e.TiposSeleccionadosJson ?? "[0]") ?? new() { 0 };
            e.TipoEjercicioActual = (TipoEjercicioLeyes)tipos[_rng.Next(tipos.Count)];

            e.SignosJson = "[]"; e.ValoresJson = "[]";
            e.OperacionesJson = "[]"; e.CoefsJson = "[]";
            e.EstructuraJson = "{}";

            switch (e.TipoEjercicioActual)
            {
                case TipoEjercicioLeyes.PuroSigno: GenerarPuroSigno(e); break;
                case TipoEjercicioLeyes.Literales: GenerarLiterales(e); break;
                case TipoEjercicioLeyes.Reales: GenerarReales(e); break;
                case TipoEjercicioLeyes.Parentesis: GenerarParentesis(e); break;
                case TipoEjercicioLeyes.Corchetes: GenerarCorchetes(e); break;
                case TipoEjercicioLeyes.Exponente: GenerarExponente(e); break;
                case TipoEjercicioLeyes.Raiz: GenerarRaiz(e); break;
                default: GenerarPuroSigno(e); break;
            }

            e.RespuestaUsuario = "";
            e.TiempoRespuesta = 0;
        }

        // ── Puro Signo ────────────────────────────────────────
        private void GenerarPuroSigno(EjercicioLeyesSignosOperacionesViewModel e)
        {
            int n = e.CantidadOperandos;
            var signos = Enumerable.Range(0, n).Select(_ => _rng.Next(2) == 0).ToList();
            var ops = Enumerable.Range(0, n - 1).Select(_ => ElegirOp(e)).ToList();

            e.SignosJson = JsonSerializer.Serialize(signos);
            e.OperacionesJson = JsonSerializer.Serialize(ops.Select(o => (int)o).ToList());

            // Calcular: para ×÷ → regla de signos. Para +− → si ambos neg → neg
            bool res = signos[0];
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i] == OperacionLeyes.Multiplicacion || ops[i] == OperacionLeyes.Division)
                    res = (res == signos[i + 1]); // igual=pos, distinto=neg
                else
                    res = res || signos[i + 1];
            }
            e.RespuestaCorrecta = res ? 1 : -1;
        }

        // ── Literales ─────────────────────────────────────────
        private void GenerarLiterales(EjercicioLeyesSignosOperacionesViewModel e)
        {
            int n = e.CantidadOperandos;
            var signos = Enumerable.Range(0, n).Select(_ => _rng.Next(2) == 0).ToList();
            var coefs = Enumerable.Range(0, n).Select(_ => GenerarEntero(e.MinDigitos, e.MaxDigitos)).ToList();
            var ops = Enumerable.Range(0, n - 1).Select(_ => ElegirOp(e, soloSumaResta: true)).ToList();

            e.Variable = "x";
            e.SignosJson = JsonSerializer.Serialize(signos);
            e.CoefsJson = JsonSerializer.Serialize(coefs);
            e.OperacionesJson = JsonSerializer.Serialize(ops.Select(o => (int)o).ToList());

            // Valor real de cada término = signo × coef
            int resultado = signos[0] ? coefs[0] : -coefs[0];
            for (int i = 0; i < ops.Count; i++)
            {
                int c = signos[i + 1] ? coefs[i + 1] : -coefs[i + 1];
                resultado = ops[i] == OperacionLeyes.Suma ? resultado + c : resultado - c;
            }
            e.CoefRespuesta = resultado;
            e.RespuestaCorrecta = resultado;
        }

        // ── Reales ────────────────────────────────────────────
        private void GenerarReales(EjercicioLeyesSignosOperacionesViewModel e)
        {
            int n = e.CantidadOperandos;
            var signos = Enumerable.Range(0, n).Select(_ => _rng.Next(2) == 0).ToList();
            var valores = Enumerable.Range(0, n)
                .Select(_ => Math.Round(_rng.NextDouble() * (Math.Pow(10, e.MaxDigitos) - 1) + 1, 1))
                .ToList();
            var ops = Enumerable.Range(0, n - 1).Select(_ => ElegirOp(e)).ToList();

            for (int i = 0; i < ops.Count; i++)
                if (ops[i] == OperacionLeyes.Division && valores[i + 1] == 0) valores[i + 1] = 1.0;

            e.SignosJson = JsonSerializer.Serialize(signos);
            e.ValoresJson = JsonSerializer.Serialize(valores);
            e.OperacionesJson = JsonSerializer.Serialize(ops.Select(o => (int)o).ToList());

            // Calcular: valor real = signo × valor absoluto
            double res = signos[0] ? valores[0] : -valores[0];
            for (int i = 0; i < ops.Count; i++)
            {
                double v = signos[i + 1] ? valores[i + 1] : -valores[i + 1];
                res = AplicarOp(ops[i], res, v);
            }
            e.RespuestaCorrecta = Math.Round(res, 2);
        }

        // ── Paréntesis ────────────────────────────────────────
        // Estructura: (vIA op vIB) opExt1 vExt1 opExt2 vExt2 ...
        // TODOS los valores se guardan con signo incluido (int con signo)
        private void GenerarParentesis(EjercicioLeyesSignosOperacionesViewModel e)
        {
            for (int intento = 0; intento < 40; intento++)
            {
                // Valores dentro del paréntesis CON signo
                int vIA = GenerarValorConSigno(e.MinDigitos, e.MaxDigitos);
                int vIB = GenerarValorConSigno(e.MinDigitos, e.MaxDigitos);
                var opI = ElegirOp(e);

                if (opI == OperacionLeyes.Division)
                {
                    if (vIB == 0) continue;
                    if (vIA % vIB != 0) continue; // solo enteros
                }

                double resP = AplicarOp(opI, vIA, vIB);
                if (resP != Math.Floor(resP)) continue;

                // Operandos externos CON signo
                int nExt = Math.Max(1, e.CantidadOperandos - 2);
                var vExts = Enumerable.Range(0, nExt)
                            .Select(_ => GenerarValorConSigno(e.MinDigitos, e.MaxDigitos))
                            .ToList();
                var opExts = Enumerable.Range(0, nExt).Select(_ => ElegirOp(e)).ToList();

                bool ok = true;
                double res = resP;
                for (int i = 0; i < nExt; i++)
                {
                    if (opExts[i] == OperacionLeyes.Division)
                    {
                        if (vExts[i] == 0 || res % vExts[i] != 0) { ok = false; break; }
                    }
                    res = AplicarOp(opExts[i], res, vExts[i]);
                    if (res != Math.Floor(res)) { ok = false; break; }
                }
                if (!ok) continue;

                e.RespuestaCorrecta = res;
                // Guardar: vIA, vIB YA tienen signo. vExts YA tienen signo.
                e.EstructuraJson = JsonSerializer.Serialize(new
                {
                    tipo = "parentesis",
                    vIA,    // int con signo, ej: -5 o 3
                    vIB,    // int con signo
                    opI = (int)opI,
                    vExts,  // List<int> con signo
                    opExts = opExts.Select(o => (int)o).ToList()
                });
                return;
            }

            // Fallback: (2+3)+(1) = 6
            e.RespuestaCorrecta = 6;
            e.EstructuraJson = JsonSerializer.Serialize(new
            {
                tipo = "parentesis",
                vIA = 2,
                vIB = 3,
                opI = 0,
                vExts = new List<int> { 1 },
                opExts = new List<int> { 0 }
            });
        }

        // ── Corchetes ─────────────────────────────────────────
        // Estructura: [(vA op1 vB) op2 vC] op3 vD
        // Todos los valores YA tienen signo incluido
        private void GenerarCorchetes(EjercicioLeyesSignosOperacionesViewModel e)
        {
            for (int intento = 0; intento < 40; intento++)
            {
                int vA = GenerarValorConSigno(e.MinDigitos, e.MaxDigitos);
                int vB = GenerarValorConSigno(e.MinDigitos, e.MaxDigitos);
                var op1 = ElegirOp(e);

                if (op1 == OperacionLeyes.Division)
                {
                    if (vB == 0 || vA % vB != 0) continue;
                }

                double resNucleo = AplicarOp(op1, vA, vB);
                if (resNucleo != Math.Floor(resNucleo)) continue;

                int vC = GenerarValorConSigno(e.MinDigitos, e.MaxDigitos);
                var op2 = ElegirOp(e);

                if (op2 == OperacionLeyes.Division)
                {
                    if (vC == 0 || resNucleo % vC != 0) continue;
                }

                double resCorch = AplicarOp(op2, resNucleo, vC);
                if (resCorch != Math.Floor(resCorch)) continue;

                bool tieneExt = e.CantidadOperandos >= 4;
                double resultado = resCorch;
                int vD = 0; var op3 = OperacionLeyes.Suma;

                if (tieneExt)
                {
                    vD = GenerarValorConSigno(e.MinDigitos, e.MaxDigitos);
                    op3 = ElegirOp(e);
                    if (op3 == OperacionLeyes.Division)
                    {
                        if (vD == 0 || resCorch % vD != 0) continue;
                    }
                    resultado = AplicarOp(op3, resCorch, vD);
                    if (resultado != Math.Floor(resultado)) continue;
                }

                e.RespuestaCorrecta = resultado;
                e.EstructuraJson = JsonSerializer.Serialize(new
                {
                    tipo = "corchetes",
                    vA,
                    vB,
                    op1 = (int)op1,
                    vC,
                    op2 = (int)op2,
                    tieneExterno = tieneExt,
                    vD,
                    op3 = (int)op3
                });
                return;
            }

            // Fallback: [(4-3)-1] = 0
            e.RespuestaCorrecta = 0;
            e.EstructuraJson = JsonSerializer.Serialize(new
            {
                tipo = "corchetes",
                vA = 4,
                vB = -3,
                op1 = 0,   // 4+(-3)=1
                vC = -1,
                op2 = 0,             // 1+(-1)=0
                tieneExterno = false,
                vD = 0,
                op3 = 0
            });
        }

        // ── Exponente ─────────────────────────────────────────
        private void GenerarExponente(EjercicioLeyesSignosOperacionesViewModel e)
        {
            e.SignoA = _rng.Next(2) == 0;
            e.ValorA = GenerarEntero(e.MinDigitos, Math.Min(e.MaxDigitos, 2));
            e.Exponente = _rng.Next(1, 4);
            e.ExpConParentesis = _rng.Next(2) == 0;
            double base_ = e.SignoA ? e.ValorA : -e.ValorA;
            e.RespuestaCorrecta = Math.Round(Math.Pow(base_, e.Exponente), 2);
        }

        // ── Raíz ─────────────────────────────────────────────
        private void GenerarRaiz(EjercicioLeyesSignosOperacionesViewModel e)
        {
            var raices = new[] { 1, 4, 9, 16, 25, 36, 49, 64, 81, 100, 121, 144, 169, 196, 225 };
            int maxVal = (int)Math.Pow(10, e.MaxDigitos) - 1;
            var validas = raices.Where(r => r <= maxVal).ToArray();
            if (validas.Length == 0) validas = new[] { 4, 9, 16 };
            e.ValorRaiz = validas[_rng.Next(validas.Length)];
            e.RespuestaCorrecta = Math.Sqrt(e.ValorRaiz);
        }

        // ════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════

        // Genera un entero CON signo aleatorio: ej -5, 3, -8, 2
        private int GenerarValorConSigno(int minDig, int maxDig)
        {
            int abs = GenerarEntero(minDig, maxDig);
            bool pos = _rng.Next(2) == 0;
            return pos ? abs : -abs;
        }

        private int GenerarEntero(int minDig, int maxDig)
        {
            int d = _rng.Next(minDig, maxDig + 1);
            return _rng.Next((int)Math.Pow(10, d - 1), (int)Math.Pow(10, d));
        }

        // Aplica operación entre dos doubles ya con signo
        private static double AplicarOp(OperacionLeyes op, double a, double b) => op switch
        {
            OperacionLeyes.Suma => a + b,
            OperacionLeyes.Resta => a - b,
            OperacionLeyes.Multiplicacion => a * b,
            OperacionLeyes.Division => b != 0 ? a / b : a,
            _ => a + b
        };

        private static string OpStr(int op) => op switch
        { 0 => "+", 1 => "−", 2 => "×", 3 => "÷", _ => "?" };

        private static string OpStr(OperacionLeyes op) => OpStr((int)op);

        // ════════════════════════════════════════════════════
        // ENUNCIADOS PARA PANTALLA DE RESULTADOS
        // ════════════════════════════════════════════════════

        private static string ConstruirEnunciadoResultado(EjercicioLeyesSignosOperacionesViewModel e)
        {
            var signos = Deser<bool>(e.SignosJson);
            var ops = Deser<int>(e.OperacionesJson);
            var coefs = Deser<int>(e.CoefsJson);
            var vals = Deser<double>(e.ValoresJson);

            switch (e.TipoEjercicioActual)
            {
                case TipoEjercicioLeyes.PuroSigno:
                    {
                        var partes = signos.Select(s => s ? "(+)" : "(−)").ToList();
                        var r = partes[0];
                        for (int i = 0; i < ops.Count; i++) r += $" {OpStr(ops[i])} {partes[i + 1]}";
                        return r;
                    }
                case TipoEjercicioLeyes.Literales:
                    {
                        if (signos.Count == 0) return "";
                        int c0 = signos[0] ? coefs[0] : -coefs[0];
                        var sb = new System.Text.StringBuilder($"{c0}{e.Variable}");
                        for (int i = 0; i < ops.Count; i++)
                        {
                            int c = signos[i + 1] ? coefs[i + 1] : -coefs[i + 1];
                            sb.Append(ops[i] == 0
                                ? (c >= 0 ? $"+{c}{e.Variable}" : $"{c}{e.Variable}")
                                : (c >= 0 ? $"−{c}{e.Variable}" : $"+{Math.Abs(c)}{e.Variable}"));
                        }
                        return sb.ToString();
                    }
                case TipoEjercicioLeyes.Reales:
                    {
                        if (signos.Count == 0) return "";
                        string V(int i) => vals[i] % 1 == 0
                            ? ((int)vals[i]).ToString()
                            : vals[i].ToString("0.#", CultureInfo.InvariantCulture);
                        double v0 = signos[0] ? vals[0] : -vals[0];
                        var sb = new System.Text.StringBuilder(v0 < 0 ? $"(−{V(0)})" : V(0));
                        for (int i = 0; i < ops.Count; i++)
                        {
                            double v = signos[i + 1] ? vals[i + 1] : -vals[i + 1];
                            sb.Append($" {OpStr(ops[i])} {(v < 0 ? $"(−{V(i + 1)})" : V(i + 1))}");
                        }
                        return sb.ToString();
                    }
                case TipoEjercicioLeyes.Exponente:
                    return e.ExpConParentesis
                        ? $"({(e.SignoA ? "" : "−")}{(int)e.ValorA})^{e.Exponente}"
                        : $"{(e.SignoA ? "" : "−")}{(int)e.ValorA}^{e.Exponente}";
                case TipoEjercicioLeyes.Raiz:
                    return $"√({(int)e.ValorRaiz})";
                case TipoEjercicioLeyes.Parentesis:
                case TipoEjercicioLeyes.Corchetes:
                    return EnunciadoDesdeEstructura(e);
                default:
                    return "";
            }
        }

        // Construye el enunciado legible desde la estructura JSON
        // Los valores YA tienen signo, solo hay que mostrarlos limpiamente
        private static string EnunciadoDesdeEstructura(EjercicioLeyesSignosOperacionesViewModel e)
        {
            if (string.IsNullOrEmpty(e.EstructuraJson) || !e.EstructuraJson.StartsWith("{"))
                return "";

            var est = JsonSerializer.Deserialize<JsonElement>(e.EstructuraJson);
            string tipo = est.GetProperty("tipo").GetString() ?? "";

            if (tipo == "parentesis")
            {
                int vIA = est.GetProperty("vIA").GetInt32();
                int vIB = est.GetProperty("vIB").GetInt32();
                int opI = est.GetProperty("opI").GetInt32();
                var vExts = est.GetProperty("vExts");
                var opExts = est.GetProperty("opExts");

                // Dentro del paréntesis: mostrar limpio
                string inner = FormatearParentesisInterno(vIA, opI, vIB);
                var sb = new System.Text.StringBuilder(inner);
                for (int i = 0; i < vExts.GetArrayLength(); i++)
                {
                    int v = vExts[i].GetInt32();
                    int op = opExts[i].GetInt32();
                    sb.Append($" {OpStr(op)} {(v < 0 ? $"(−{Math.Abs(v)})" : v.ToString())}");
                }
                return sb.ToString();
            }
            else // corchetes
            {
                int vA = est.GetProperty("vA").GetInt32();
                int vB = est.GetProperty("vB").GetInt32();
                int op1 = est.GetProperty("op1").GetInt32();
                int vC = est.GetProperty("vC").GetInt32();
                int op2 = est.GetProperty("op2").GetInt32();
                bool ext = est.GetProperty("tieneExterno").GetBoolean();

                string inner = FormatearParentesisInterno(vA, op1, vB);
                string cStr = vC < 0 ? $"(−{Math.Abs(vC)})" : vC.ToString();
                string bracket = $"[{inner}{OpStr(op2)}{cStr}]";

                if (!ext) return bracket;

                int vD = est.GetProperty("vD").GetInt32();
                int op3 = est.GetProperty("op3").GetInt32();
                string dStr = vD < 0 ? $"(−{Math.Abs(vD)})" : vD.ToString();
                return $"{{{bracket}{OpStr(op3)}{dStr}}}";
            }
        }

        // Formatea el interior de un paréntesis: "5−3" o "−2+7"
        private static string FormatearParentesisInterno(int a, int op, int b)
        {
            // a ya tiene signo. El operador es op. b ya tiene signo.
            // Mostrar: (a op |b|) donde el signo de b lo da el operador visualmente
            string aStr = a < 0 ? $"−{Math.Abs(a)}" : a.ToString();

            // Si op=Suma: mostrar +b o -b según signo de b
            // Si op=Resta: mostrar -b o +b (inverso)
            // Si op=Mult/Div: mostrar × o ÷ con valor absoluto de b
            string resto;
            if (op == 0) // Suma: a + b efectivamente
                resto = b >= 0 ? $"+{b}" : $"−{Math.Abs(b)}";
            else if (op == 1) // Resta: a - b efectivamente
                resto = b >= 0 ? $"−{b}" : $"+{Math.Abs(b)}";
            else // Mult/Div
                resto = $"{OpStr(op)}{(b < 0 ? $"(−{Math.Abs(b)})" : b.ToString())}";

            return $"({aStr}{resto})";
        }

        private static List<T> Deser<T>(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "[]") return new();
            return JsonSerializer.Deserialize<List<T>>(json) ?? new();
        }

        private IActionResult VistaResultado(
            List<ResultadoLeyesSignosOperaciones> lista,
            EjercicioLeyesSignosOperacionesViewModel e)
        {
            return View("ResultadoLeyesSignosOperaciones",
                new ResultadoFinalLeyesSignosOperacionesViewModel
                {
                    Resultados = lista,
                    CantidadEjercicios = e.CantidadEjercicios,
                    CantidadOperandos = e.CantidadOperandos,
                    TipoEjercicio = e.TipoEjercicioActual,
                    VelocidadEjercicio = e.VelocidadEjercicio,
                    MinDigitos = e.MinDigitos,
                    MaxDigitos = e.MaxDigitos
                });
        }

        private static List<ResultadoLeyesSignosOperaciones> Deserializar(string json)
        {
            if (string.IsNullOrEmpty(json)) return new();
            return JsonSerializer.Deserialize<List<ResultadoLeyesSignosOperaciones>>(json) ?? new();
        }
    }
}
