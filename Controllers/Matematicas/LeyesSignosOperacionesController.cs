using AnzanMegaArithmetics.Models.MatematicasModels.LeyesSignosOperaciones;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers.Matematicas
{
    public class LeyesSignosOperacionesController : Controller
    {
        private static readonly Random _rng = new();

        private static readonly TipoEjercicioLeyes[] _tiposAleatorios =
        {
            TipoEjercicioLeyes.PuroSigno,
            TipoEjercicioLeyes.Literales,
            TipoEjercicioLeyes.Reales,
            TipoEjercicioLeyes.Parentesis,
            TipoEjercicioLeyes.Exponente,
            TipoEjercicioLeyes.Corchetes,
            TipoEjercicioLeyes.Raiz
        };

        // ════════════════════════════════════════════════════
        // ACCIONES
        // ════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult Formulario() =>
            View("FormularioLeyesSignosOperaciones",
                 new ConfLeyesSignosOperacionesViewModel());

        [HttpPost]
        public IActionResult Iniciar(ConfLeyesSignosOperacionesViewModel config)
        {
            if (!ModelState.IsValid)
                return View("FormularioLeyesSignosOperaciones", config);

            config.CantidadEjercicios = Math.Clamp(config.CantidadEjercicios, 1, 100);
            config.CantidadOperandos = Math.Clamp(config.CantidadOperandos, 2, 10);
            config.MinDigitos = Math.Clamp(config.MinDigitos, 1, 3);
            config.MaxDigitos = Math.Clamp(config.MaxDigitos, config.MinDigitos, 3);

            var estado = new EjercicioLeyesSignosOperacionesViewModel
            {
                CantidadEjercicios = config.CantidadEjercicios,
                CantidadOperandos = config.CantidadOperandos,
                MinDigitos = config.MinDigitos,
                MaxDigitos = config.MaxDigitos,
                TipoEjercicioConfig = config.TipoEjercicio,
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
                TipoEjercicioConfig = TipoEjercicioLeyes.PuroSigno,
                VelocidadEjercicio = "0",
                EjerciciosRealizados = 0,
                ResultadosJson = "[]",
                EsTutorial = true,
                PasoTutorial = 0
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
        // GENERADORES
        // ════════════════════════════════════════════════════

        private void GenerarEjercicio(EjercicioLeyesSignosOperacionesViewModel e)
        {
            if (e.TipoEjercicioConfig == TipoEjercicioLeyes.Aleatorio)
                e.TipoEjercicioActual = _tiposAleatorios[_rng.Next(_tiposAleatorios.Length)];
            else
                e.TipoEjercicioActual = e.TipoEjercicioConfig;

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
        // Múltiples operandos: solo signos, respuesta es el signo del resultado
        private void GenerarPuroSigno(EjercicioLeyesSignosOperacionesViewModel e)
        {
            int n = e.CantidadOperandos;
            var signos = Enumerable.Range(0, n).Select(_ => _rng.Next(2) == 0).ToList();
            var ops = Enumerable.Range(0, n - 1)
                .Select(_ => _rng.Next(2) == 0 ? OperacionLeyes.Multiplicacion : OperacionLeyes.Division)
                .ToList();

            e.SignosJson = JsonSerializer.Serialize(signos);
            e.OperacionesJson = JsonSerializer.Serialize(ops);
            e.ValoresJson = "[]";

            // Para mult/div: par de negativos → positivo
            // Aplicar de izquierda a derecha
            bool resPositivo = signos[0];
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i] == OperacionLeyes.Multiplicacion || ops[i] == OperacionLeyes.Division)
                    resPositivo = resPositivo == signos[i + 1];
                else if (ops[i] == OperacionLeyes.Suma)
                    resPositivo = resPositivo || signos[i + 1];
                else
                    resPositivo = resPositivo;
            }

            e.RespuestaCorrecta = resPositivo ? 1 : -1;
            e.EstructuraJson = ConstruirEnunciadoPuroSigno(signos, ops);
        }

        // ── Literales ─────────────────────────────────────────
        // Múltiples términos: 3x - 5x + 2x = ?x
        private void GenerarLiterales(EjercicioLeyesSignosOperacionesViewModel e)
        {
            int n = e.CantidadOperandos;
            var signos = Enumerable.Range(0, n).Select(_ => _rng.Next(2) == 0).ToList();
            var coefs = Enumerable.Range(0, n).Select(_ => GenerarEntero(e.MinDigitos, e.MaxDigitos)).ToList();
            var ops = Enumerable.Range(0, n - 1)
                .Select(_ => _rng.Next(2) == 0 ? OperacionLeyes.Suma : OperacionLeyes.Resta)
                .ToList();

            e.Variable = "x";
            e.SignosJson = JsonSerializer.Serialize(signos);
            e.CoefsJson = JsonSerializer.Serialize(coefs);
            e.OperacionesJson = JsonSerializer.Serialize(ops);

            // Calcular coeficiente resultado
            int resultado = signos[0] ? coefs[0] : -coefs[0];
            for (int i = 0; i < ops.Count; i++)
            {
                int c = signos[i + 1] ? coefs[i + 1] : -coefs[i + 1];
                resultado = ops[i] == OperacionLeyes.Suma ? resultado + c : resultado - c;
            }

            e.CoefRespuesta = resultado;
            e.RespuestaCorrecta = resultado;
            e.EstructuraJson = ConstruirEnunciadoLiteral(signos, coefs, ops, e.Variable);
        }

        // ── Reales ────────────────────────────────────────────
        // Múltiples operandos con decimales
        private void GenerarReales(EjercicioLeyesSignosOperacionesViewModel e)
        {
            int n = e.CantidadOperandos;
            var signos = Enumerable.Range(0, n).Select(_ => _rng.Next(2) == 0).ToList();
            var valores = Enumerable.Range(0, n)
                .Select(_ => Math.Round(_rng.NextDouble() * (Math.Pow(10, e.MaxDigitos) - 1) + 1, 1))
                .ToList();
            var ops = Enumerable.Range(0, n - 1).Select(_ => ElegirOp(false)).ToList();

            // Evitar divisiones por cero
            for (int i = 0; i < ops.Count; i++)
                if (ops[i] == OperacionLeyes.Division && valores[i + 1] == 0)
                    valores[i + 1] = 1.0;

            e.SignosJson = JsonSerializer.Serialize(signos);
            e.ValoresJson = JsonSerializer.Serialize(valores);
            e.OperacionesJson = JsonSerializer.Serialize(ops);

            double resultado = signos[0] ? valores[0] : -valores[0];
            for (int i = 0; i < ops.Count; i++)
            {
                double v = signos[i + 1] ? valores[i + 1] : -valores[i + 1];
                resultado = ops[i] switch
                {
                    OperacionLeyes.Suma => resultado + v,
                    OperacionLeyes.Resta => resultado - v,
                    OperacionLeyes.Multiplicacion => resultado * v,
                    OperacionLeyes.Division => Math.Round(resultado / v, 2),
                    _ => resultado + v
                };
            }

            e.RespuestaCorrecta = Math.Round(resultado, 2);
            e.EstructuraJson = ConstruirEnunciadoReales(signos, valores, ops);
        }

        // ── Paréntesis ────────────────────────────────────────
        // Estructura: (A op B) op C op D...
        // El primer grupo va entre paréntesis, luego opera con el resto
        private void GenerarParentesis(EjercicioLeyesSignosOperacionesViewModel e)
        {
            // Grupo interno (entre paréntesis): siempre 2 operandos
            var signosInternos = new List<bool> { _rng.Next(2) == 0, _rng.Next(2) == 0 };
            var valsInternos = new List<int>  { GenerarEntero(e.MinDigitos, e.MaxDigitos),
                                                  GenerarEntero(e.MinDigitos, e.MaxDigitos) };
            var opInterna = ElegirOp(false);
            if (opInterna == OperacionLeyes.Division && valsInternos[1] == 0)
                valsInternos[1] = 1;

            double rIA = signosInternos[0] ? valsInternos[0] : -valsInternos[0];
            double rIB = signosInternos[1] ? valsInternos[1] : -valsInternos[1];
            double resInterno = opInterna switch
            {
                OperacionLeyes.Suma => rIA + rIB,
                OperacionLeyes.Resta => rIA - rIB,
                OperacionLeyes.Multiplicacion => rIA * rIB,
                OperacionLeyes.Division => Math.Round(rIA / rIB, 2),
                _ => rIA + rIB
            };

            // Operandos externos (el resto de CantidadOperandos - 2, mínimo 1 externo)
            int nExt = Math.Max(1, e.CantidadOperandos - 2);
            var signosExt = Enumerable.Range(0, nExt).Select(_ => _rng.Next(2) == 0).ToList();
            var valsExt = Enumerable.Range(0, nExt)
                .Select(_ => GenerarEntero(e.MinDigitos, e.MaxDigitos)).ToList();
            var opsExt = Enumerable.Range(0, nExt).Select(_ => ElegirOp(false)).ToList();
            for (int i = 0; i < opsExt.Count; i++)
                if (opsExt[i] == OperacionLeyes.Division && valsExt[i] == 0)
                    valsExt[i] = 1;

            double resultado = resInterno;
            for (int i = 0; i < nExt; i++)
            {
                double v = signosExt[i] ? valsExt[i] : -valsExt[i];
                resultado = opsExt[i] switch
                {
                    OperacionLeyes.Suma => resultado + v,
                    OperacionLeyes.Resta => resultado - v,
                    OperacionLeyes.Multiplicacion => resultado * v,
                    OperacionLeyes.Division => v != 0 ? Math.Round(resultado / v, 2) : resultado,
                    _ => resultado + v
                };
            }

            e.RespuestaCorrecta = Math.Round(resultado, 2);

            // Serializar estructura para la vista
            var estructura = new
            {
                tipo = "parentesis",
                signosInternos,
                valsInternos,
                opInterna = (int)opInterna,
                signosExt,
                valsExt,
                opsExt = opsExt.Select(o => (int)o).ToList()
            };
            e.EstructuraJson = JsonSerializer.Serialize(estructura);
        }

        // ── Corchetes ─────────────────────────────────────────
        // Estructura: {[A op B] op C} op D
        // Nivel 1: corchetes, Nivel 2: paréntesis dentro
        private void GenerarCorchetes(EjercicioLeyesSignosOperacionesViewModel e)
        {
            // Núcleo: (a op b)
            bool sA = _rng.Next(2) == 0, sB = _rng.Next(2) == 0;
            int vA = GenerarEntero(e.MinDigitos, e.MaxDigitos);
            int vB = GenerarEntero(e.MinDigitos, e.MaxDigitos);
            var op1 = ElegirOp(false);
            if (op1 == OperacionLeyes.Division && vB == 0) vB = 1;

            double rA = sA ? vA : -vA, rB = sB ? vB : -vB;
            double resParens = op1 switch
            {
                OperacionLeyes.Suma => rA + rB,
                OperacionLeyes.Resta => rA - rB,
                OperacionLeyes.Multiplicacion => rA * rB,
                OperacionLeyes.Division => Math.Round(rA / rB, 2),
                _ => rA + rB
            };

            // Nivel corchete: [resultado op c]
            bool sC = _rng.Next(2) == 0;
            int vC = GenerarEntero(e.MinDigitos, e.MaxDigitos);
            var op2 = ElegirOp(false);
            if (op2 == OperacionLeyes.Division && vC == 0) vC = 1;
            double rC = sC ? vC : -vC;
            double resCorchete = op2 switch
            {
                OperacionLeyes.Suma => resParens + rC,
                OperacionLeyes.Resta => resParens - rC,
                OperacionLeyes.Multiplicacion => resParens * rC,
                OperacionLeyes.Division => rC != 0 ? Math.Round(resParens / rC, 2) : resParens,
                _ => resParens + rC
            };

            // Nivel externo: {resultado op d} (solo si hay suficientes operandos)
            double resultado = resCorchete;
            bool tieneExterno = e.CantidadOperandos >= 4;
            bool sD = false; int vD = 0; OperacionLeyes op3 = OperacionLeyes.Suma;
            if (tieneExterno)
            {
                sD = _rng.Next(2) == 0;
                vD = GenerarEntero(e.MinDigitos, e.MaxDigitos);
                op3 = ElegirOp(false);
                if (op3 == OperacionLeyes.Division && vD == 0) vD = 1;
                double rD = sD ? vD : -vD;
                resultado = op3 switch
                {
                    OperacionLeyes.Suma => resCorchete + rD,
                    OperacionLeyes.Resta => resCorchete - rD,
                    OperacionLeyes.Multiplicacion => resCorchete * rD,
                    OperacionLeyes.Division => rD != 0 ? Math.Round(resCorchete / rD, 2) : resCorchete,
                    _ => resCorchete + rD
                };
            }

            e.RespuestaCorrecta = Math.Round(resultado, 2);

            var estructura = new
            {
                tipo = "corchetes",
                sA,
                vA,
                sB,
                vB,
                op1 = (int)op1,
                sC,
                vC,
                op2 = (int)op2,
                tieneExterno,
                sD,
                vD,
                op3 = (int)op3
            };
            e.EstructuraJson = JsonSerializer.Serialize(estructura);
        }

        // ── Exponente ─────────────────────────────────────────
        private void GenerarExponente(EjercicioLeyesSignosOperacionesViewModel e)
        {
            e.SignoA = _rng.Next(2) == 0;
            e.ValorA = GenerarEntero(e.MinDigitos, Math.Min(e.MaxDigitos, 2));
            e.Exponente = _rng.Next(2, 5);
            double base_ = e.SignoA ? e.ValorA : -e.ValorA;
            e.RespuestaCorrecta = Math.Round(Math.Pow(base_, e.Exponente), 2);
            string sa = e.SignoA ? "+" : "−";
            e.EstructuraJson = $"({sa}{(int)e.ValorA})^{e.Exponente}";
        }

        // ── Raíz cuadrada ─────────────────────────────────────
        // √(valor) = ? donde valor siempre es un cuadrado perfecto
        private void GenerarRaiz(EjercicioLeyesSignosOperacionesViewModel e)
        {
            // Generar raíces perfectas: 1,4,9,16,25,36,49,64,81,100,121,144,169,196,225
            var raicesPerfectas = new[] { 1, 4, 9, 16, 25, 36, 49, 64, 81, 100, 121, 144, 169, 196, 225 };
            // Filtrar según dígitos
            int maxVal = (int)Math.Pow(10, e.MaxDigitos) - 1;
            var validas = raicesPerfectas.Where(r => r <= maxVal && r >= 1).ToArray();
            if (validas.Length == 0) validas = new[] { 4, 9, 16 };

            e.ValorRaiz = validas[_rng.Next(validas.Length)];
            e.RespuestaCorrecta = Math.Sqrt(e.ValorRaiz);
            e.EstructuraJson = $"√({(int)e.ValorRaiz})";
        }

        // ── Helpers ──────────────────────────────────────────

        private OperacionLeyes ElegirOp(bool potencia)
        {
            var ops = new List<OperacionLeyes>
            {
                OperacionLeyes.Suma, OperacionLeyes.Resta,
                OperacionLeyes.Multiplicacion, OperacionLeyes.Division
            };
            if (potencia) ops.Add(OperacionLeyes.Potencia);
            return ops[_rng.Next(ops.Count)];
        }

        private int GenerarEntero(int minDig, int maxDig)
        {
            int d = _rng.Next(minDig, maxDig + 1);
            return _rng.Next((int)Math.Pow(10, d - 1), (int)Math.Pow(10, d));
        }

        private static string OpStr(OperacionLeyes op) => op switch
        {
            OperacionLeyes.Suma => "+",
            OperacionLeyes.Resta => "−",
            OperacionLeyes.Multiplicacion => "×",
            OperacionLeyes.Division => "÷",
            OperacionLeyes.Potencia => "^",
            _ => "?"
        };

        private static string ConstruirEnunciadoPuroSigno(List<bool> signos, List<OperacionLeyes> ops)
        {
            var parts = signos.Select(s => s ? "(+)" : "(−)").ToList();
            var result = parts[0];
            for (int i = 0; i < ops.Count; i++)
                result += $" {OpStr(ops[i])} {parts[i + 1]}";
            return result;
        }

        private static string ConstruirEnunciadoLiteral(
            List<bool> signos, List<int> coefs, List<OperacionLeyes> ops, string variable)
        {
            var parts = signos.Select((s, i) => $"({(s ? "+" : "−")}{coefs[i]}{variable})").ToList();
            var result = parts[0];
            for (int i = 0; i < ops.Count; i++)
                result += $" {OpStr(ops[i])} {parts[i + 1]}";
            return result;
        }

        private static string ConstruirEnunciadoReales(
            List<bool> signos, List<double> valores, List<OperacionLeyes> ops)
        {
            var parts = signos.Select((s, i) =>
            {
                string v = valores[i] % 1 == 0
                    ? ((int)valores[i]).ToString()
                    : valores[i].ToString("0.#", CultureInfo.InvariantCulture);
                return $"({(s ? "+" : "−")}{v})";
            }).ToList();
            var result = parts[0];
            for (int i = 0; i < ops.Count; i++)
                result += $" {OpStr(ops[i])} {parts[i + 1]}";
            return result;
        }


        /// <summary>
        /// Construye el texto del enunciado para mostrar en resultados.
        /// Para Paréntesis y Corchetes usa EstructuraJson para armar el texto legible.
        /// </summary>
        private static string ConstruirEnunciadoResultado(EjercicioLeyesSignosOperacionesViewModel e)
        {
            switch (e.TipoEjercicioActual)
            {
                case TipoEjercicioLeyes.PuroSigno:
                    {
                        var signos = System.Text.Json.JsonSerializer.Deserialize<List<bool>>(e.SignosJson ?? "[]") ?? new();
                        var ops = System.Text.Json.JsonSerializer.Deserialize<List<int>>(e.OperacionesJson ?? "[]") ?? new();
                        return ConstruirEnunciadoPuroSigno(signos, ops.Select(o => (OperacionLeyes)o).ToList());
                    }
                case TipoEjercicioLeyes.Literales:
                    {
                        var signos = System.Text.Json.JsonSerializer.Deserialize<List<bool>>(e.SignosJson ?? "[]") ?? new();
                        var coefs = System.Text.Json.JsonSerializer.Deserialize<List<int>>(e.CoefsJson ?? "[]") ?? new();
                        var ops = System.Text.Json.JsonSerializer.Deserialize<List<int>>(e.OperacionesJson ?? "[]") ?? new();
                        return ConstruirEnunciadoLiteral(signos, coefs, ops.Select(o => (OperacionLeyes)o).ToList(), e.Variable);
                    }
                case TipoEjercicioLeyes.Reales:
                    {
                        var signos = System.Text.Json.JsonSerializer.Deserialize<List<bool>>(e.SignosJson ?? "[]") ?? new();
                        var valores = System.Text.Json.JsonSerializer.Deserialize<List<double>>(e.ValoresJson ?? "[]") ?? new();
                        var ops = System.Text.Json.JsonSerializer.Deserialize<List<int>>(e.OperacionesJson ?? "[]") ?? new();
                        return ConstruirEnunciadoReales(signos, valores, ops.Select(o => (OperacionLeyes)o).ToList());
                    }
                case TipoEjercicioLeyes.Exponente:
                    {
                        string sa = e.SignoA ? "+" : "−";
                        return $"({sa}{(int)e.ValorA})^{e.Exponente}";
                    }
                case TipoEjercicioLeyes.Raiz:
                    return $"√({(int)e.ValorRaiz})";
                case TipoEjercicioLeyes.Parentesis:
                    {
                        if (string.IsNullOrEmpty(e.EstructuraJson) || !e.EstructuraJson.StartsWith("{"))
                            return e.EstructuraJson ?? "";
                        var est = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(e.EstructuraJson);
                        var sIA = est.GetProperty("signosInternos")[0].GetBoolean();
                        var sIB = est.GetProperty("signosInternos")[1].GetBoolean();
                        var vIA = est.GetProperty("valsInternos")[0].GetInt32();
                        var vIB = est.GetProperty("valsInternos")[1].GetInt32();
                        var opI = (OperacionLeyes)est.GetProperty("opInterna").GetInt32();
                        var opsE = est.GetProperty("opsExt");
                        var vExt = est.GetProperty("valsExt");
                        var sExt = est.GetProperty("signosExt");
                        var txt = $"({(sIA ? "+" : "−")}{vIA} {OpStr(opI)} {(sIB ? "+" : "−")}{vIB})";
                        for (int i = 0; i < opsE.GetArrayLength(); i++)
                            txt += $" {OpStr((OperacionLeyes)opsE[i].GetInt32())} ({(sExt[i].GetBoolean() ? "+" : "−")}{vExt[i].GetInt32()})";
                        return txt;
                    }
                case TipoEjercicioLeyes.Corchetes:
                    {
                        if (string.IsNullOrEmpty(e.EstructuraJson) || !e.EstructuraJson.StartsWith("{"))
                            return e.EstructuraJson ?? "";
                        var est = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(e.EstructuraJson);
                        var sA2 = est.GetProperty("sA").GetBoolean();
                        var vA2 = est.GetProperty("vA").GetInt32();
                        var sB2 = est.GetProperty("sB").GetBoolean();
                        var vB2 = est.GetProperty("vB").GetInt32();
                        var op1 = (OperacionLeyes)est.GetProperty("op1").GetInt32();
                        var sC = est.GetProperty("sC").GetBoolean();
                        var vC = est.GetProperty("vC").GetInt32();
                        var op2 = (OperacionLeyes)est.GetProperty("op2").GetInt32();
                        var tieneExt = est.GetProperty("tieneExterno").GetBoolean();
                        var inner = $"({(sA2 ? "+" : "−")}{vA2} {OpStr(op1)} {(sB2 ? "+" : "−")}{vB2})";
                        var bracket = $"[{inner} {OpStr(op2)} {(sC ? "+" : "−")}{vC}]";
                        if (!tieneExt) return bracket;
                        var sD = est.GetProperty("sD").GetBoolean();
                        var vD = est.GetProperty("vD").GetInt32();
                        var op3 = (OperacionLeyes)est.GetProperty("op3").GetInt32();
                        return $"{{{bracket} {OpStr(op3)} {(sD ? "+" : "−")}{vD}}}";
                    }
                default:
                    return e.EstructuraJson ?? "";
            }
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
                    TipoEjercicio = e.TipoEjercicioConfig,
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
