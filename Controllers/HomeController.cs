using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OnpeWeb.Models;
using System.Data;

namespace OnpeWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _con;

        public HomeController(IConfiguration config)
        {
            _con = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión");
        }

        public IActionResult Index() => View();

        public IActionResult Participacion()
        {
            var model = new ParticipacionTotalViewModel { Ambito = "Nacional" };
            try
            {
                using var cn = new SqlConnection(_con);
                cn.Open();
                using (var cmd = new SqlCommand("SELECT * FROM vTotalVotos", cn))
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        model.TotalAsistentes = dr.GetInt32(0);
                        model.PorcAsistentes = dr.IsDBNull(1) ? "0.000 %" : dr.GetString(1);
                        model.TotalAusentes = dr.GetInt32(2);
                        model.PorcAusentes = dr.IsDBNull(3) ? "0.000 %" : dr.GetString(3);
                        model.ElectoresHabiles = dr.GetInt32(4);
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar datos de participación: " + ex.Message;
            }
            return View(model);
        }

        public IActionResult ParticipacionTotal()
        {
            var model = new ParticipacionTotalViewModel { Ambito = "Nacional" };
            try
            {
                using var cn = new SqlConnection(_con);
                cn.Open();

                using (var cmd = new SqlCommand("SELECT * FROM vTotalVotos", cn))
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        model.TotalAsistentes = dr.GetInt32(0);
                        model.PorcAsistentes = dr.IsDBNull(1) ? "0.000 %" : dr.GetString(1);
                        model.TotalAusentes = dr.GetInt32(2);
                        model.PorcAusentes = dr.IsDBNull(3) ? "0.000 %" : dr.GetString(3);
                        model.ElectoresHabiles = dr.GetInt32(4);
                    }
                }

                using (var cmd = new SqlCommand("usp_getVotos", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@inicio", 1);
                    cmd.Parameters.AddWithValue("@fin", 25);
                    using var dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        model.ListaDepartamentos.Add(new DatoResultado
                        {
                            Columna1 = dr["DPD"]?.ToString() ?? "",
                            Columna2 = dr["TV"]?.ToString() ?? "0",
                            Columna3 = dr["PTV"]?.ToString() ?? "0.000 %",
                            Columna4 = dr["TA"]?.ToString() ?? "0",
                            Columna5 = dr["PTA"]?.ToString() ?? "0.000 %",
                            Columna6 = dr["EH"]?.ToString() ?? "0"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar datos nacionales: " + ex.Message;
            }
            return View("ParticipacionDetalle", model);
        }

        public IActionResult ParticipacionExtranjero()
        {
            var model = new ParticipacionTotalViewModel { Ambito = "Extranjero" };
            try
            {
                using var cn = new SqlConnection(_con);
                cn.Open();

                using (var cmd = new SqlCommand("usp_getVotosExtranjero", cn) { CommandType = CommandType.StoredProcedure })
                {
                    using var dr = cmd.ExecuteReader();
                    int sumAsistentes = 0;
                    int sumAusentes = 0;
                    int sumElectores = 0;

                    while (dr.Read())
                    {
                        var item = new DatoResultado
                        {
                            Columna1 = dr["DPD"]?.ToString() ?? "",
                            Columna2 = dr["TV"]?.ToString() ?? "0",
                            Columna3 = dr["PTV"]?.ToString() ?? "0.000 %",
                            Columna4 = dr["TA"]?.ToString() ?? "0",
                            Columna5 = dr["PTA"]?.ToString() ?? "0.000 %",
                            Columna6 = dr["EH"]?.ToString() ?? "0"
                        };
                        model.ListaDepartamentos.Add(item);

                        sumAsistentes += int.TryParse(item.Columna2, out int tv) ? tv : 0;
                        sumAusentes += int.TryParse(item.Columna4, out int ta) ? ta : 0;
                        sumElectores += int.TryParse(item.Columna6, out int eh) ? eh : 0;
                    }

                    model.TotalAsistentes = sumAsistentes;
                    model.TotalAusentes = sumAusentes;
                    model.ElectoresHabiles = sumElectores;

                    if (sumElectores > 0)
                    {
                        double pAsist = (sumAsistentes * 100.0 / sumElectores);
                        double pAus = (sumAusentes * 100.0 / sumElectores);
                        model.PorcAsistentes = pAsist.ToString("0.000") + " %";
                        model.PorcAusentes = pAus.ToString("0.000") + " %";
                    }
                    else
                    {
                        model.PorcAsistentes = "0.000 %";
                        model.PorcAusentes = "0.000 %";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar datos de Extranjero: " + ex.Message;
            }
            return View("ParticipacionDetalle", model);
        }

        public IActionResult ParticipacionDetalle(
            string ambito = "Nacional",
            string depa = null,
            string prov = null,
            string dist = null,
            string siguiente = null)
        {
            var model = new ParticipacionTotalViewModel
            {
                Ambito = ambito,
                DepartamentoSeleccionado = depa,
                ProvinciaSeleccionada = prov,
                DistritoSeleccionado = dist
            };

            try
            {
                using var cn = new SqlConnection(_con);
                cn.Open();

                string nivelActual = !string.IsNullOrEmpty(dist) ? "Distrito" :
                                     !string.IsNullOrEmpty(prov) ? "Provincia" :
                                     !string.IsNullOrEmpty(depa) ? "Departamento" : "Total";

                if (!string.IsNullOrEmpty(siguiente))
                {
                    if (nivelActual == "Total")
                    {
                        model.DepartamentoSeleccionado = siguiente;
                        using (var cmd = new SqlCommand("usp_getVotosDepartamento", cn) { CommandType = CommandType.StoredProcedure })
                        {
                            cmd.Parameters.AddWithValue("@Departamento", siguiente);
                            using var dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                model.ListaDepartamentos.Add(new DatoResultado
                                {
                                    Columna1 = dr["DPD"]?.ToString() ?? "",
                                    Columna2 = dr["TV"]?.ToString() ?? "0",
                                    Columna3 = dr["PTV"]?.ToString() ?? "0.000 %",
                                    Columna4 = dr["TA"]?.ToString() ?? "0",
                                    Columna5 = dr["PTA"]?.ToString() ?? "0.000 %",
                                    Columna6 = dr["EH"]?.ToString() ?? "0"
                                });
                            }
                        }
                    }
                    else if (nivelActual == "Departamento")
                    {
                        model.ProvinciaSeleccionada = siguiente;
                        using (var cmd = new SqlCommand("usp_getVotosProvincia", cn) { CommandType = CommandType.StoredProcedure })
                        {
                            cmd.Parameters.AddWithValue("@Provincia", siguiente);
                            using var dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                model.ListaDepartamentos.Add(new DatoResultado
                                {
                                    Columna1 = dr["DPD"]?.ToString() ?? "",
                                    Columna2 = dr["TV"]?.ToString() ?? "0",
                                    Columna3 = dr["PTV"]?.ToString() ?? "0.000 %",
                                    Columna4 = dr["TA"]?.ToString() ?? "0",
                                    Columna5 = dr["PTA"]?.ToString() ?? "0.000 %",
                                    Columna6 = dr["EH"]?.ToString() ?? "0"
                                });
                            }
                        }
                    }
                    else if (nivelActual == "Provincia")
                    {
                        model.DistritoSeleccionado = siguiente;

                        using (var cmd = new SqlCommand("usp_getVotosProvincia", cn) { CommandType = CommandType.StoredProcedure })
                        {
                            cmd.Parameters.AddWithValue("@Provincia", model.ProvinciaSeleccionada ?? "");
                            using var dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                model.ListaDepartamentos.Add(new DatoResultado
                                {
                                    Columna1 = dr["DPD"]?.ToString()?.Trim() ?? "",
                                    Columna2 = dr["TV"]?.ToString() ?? "0",
                                    Columna3 = dr["PTV"]?.ToString() ?? "0.000 %",
                                    Columna4 = dr["TA"]?.ToString() ?? "0",
                                    Columna5 = dr["PTA"]?.ToString() ?? "0.000 %",
                                    Columna6 = dr["EH"]?.ToString() ?? "0"
                                });
                            }
                        }

                        if (!string.IsNullOrEmpty(model.DistritoSeleccionado))
                        {
                            var distritoSeleccionado = model.DistritoSeleccionado.Trim().ToUpperInvariant();

                            var filaFiltrada = model.ListaDepartamentos
                                .Where(d => (d.Columna1?.Trim().ToUpperInvariant() ?? "") == distritoSeleccionado)
                                .ToList();

                            if (filaFiltrada.Any())
                            {
                                model.ListaDepartamentos = filaFiltrada;
                            }
                            else
                            {
                                ViewBag.MensajeDistrito = $"No se encontró coincidencia exacta para '{model.DistritoSeleccionado}'. Mostrando datos de la provincia.";
                            }
                        }
                    }
                    else if (nivelActual == "Distrito")
                    {
                        model.DistritoSeleccionado = siguiente;

                        using (var cmd = new SqlCommand("usp_getVotosProvincia", cn) { CommandType = CommandType.StoredProcedure })
                        {
                            cmd.Parameters.AddWithValue("@Provincia", model.ProvinciaSeleccionada ?? "");
                            using var dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                model.ListaDepartamentos.Add(new DatoResultado
                                {
                                    Columna1 = dr["DPD"]?.ToString()?.Trim() ?? "",
                                    Columna2 = dr["TV"]?.ToString() ?? "0",
                                    Columna3 = dr["PTV"]?.ToString() ?? "0.000 %",
                                    Columna4 = dr["TA"]?.ToString() ?? "0",
                                    Columna5 = dr["PTA"]?.ToString() ?? "0.000 %",
                                    Columna6 = dr["EH"]?.ToString() ?? "0"
                                });
                            }
                        }

                        if (!string.IsNullOrEmpty(model.DistritoSeleccionado))
                        {
                            var distritoSeleccionado = model.DistritoSeleccionado.Trim().ToUpperInvariant();
                            var filaFiltrada = model.ListaDepartamentos
                                .Where(d => (d.Columna1?.Trim().ToUpperInvariant() ?? "") == distritoSeleccionado)
                                .ToList();

                            if (filaFiltrada.Any())
                            {
                                model.ListaDepartamentos = filaFiltrada;
                            }
                        }
                    }

                    model.TotalAsistentes = model.ListaDepartamentos.Sum(x => int.TryParse(x.Columna2, out int v) ? v : 0);
                    model.TotalAusentes = model.ListaDepartamentos.Sum(x => int.TryParse(x.Columna4, out int v) ? v : 0);
                    model.ElectoresHabiles = model.ListaDepartamentos.Sum(x => int.TryParse(x.Columna6, out int v) ? v : 0);

                    if (model.ElectoresHabiles > 0)
                    {
                        model.PorcAsistentes = ((double)model.TotalAsistentes / model.ElectoresHabiles * 100).ToString("0.000") + " %";
                        model.PorcAusentes = ((double)model.TotalAusentes / model.ElectoresHabiles * 100).ToString("0.000") + " %";
                    }
                    else
                    {
                        model.PorcAsistentes = "0.000 %";
                        model.PorcAusentes = "0.000 %";
                    }
                }
                else
                {
                    if (ambito == "Extranjero")
                    {
                        using (var cmd = new SqlCommand("usp_getVotosExtranjero", cn) { CommandType = CommandType.StoredProcedure })
                        {
                            using var dr = cmd.ExecuteReader();
                            int sumAsist = 0, sumAus = 0, sumElec = 0;
                            while (dr.Read())
                            {
                                var item = new DatoResultado
                                {
                                    Columna1 = dr["DPD"]?.ToString() ?? "",
                                    Columna2 = dr["TV"]?.ToString() ?? "0",
                                    Columna3 = dr["PTV"]?.ToString() ?? "0.000 %",
                                    Columna4 = dr["TA"]?.ToString() ?? "0",
                                    Columna5 = dr["PTA"]?.ToString() ?? "0.000 %",
                                    Columna6 = dr["EH"]?.ToString() ?? "0"
                                };
                                model.ListaDepartamentos.Add(item);
                                sumAsist += int.TryParse(item.Columna2, out int v) ? v : 0;
                                sumAus += int.TryParse(item.Columna4, out v) ? v : 0;
                                sumElec += int.TryParse(item.Columna6, out v) ? v : 0;
                            }
                            model.TotalAsistentes = sumAsist;
                            model.TotalAusentes = sumAus;
                            model.ElectoresHabiles = sumElec;
                            if (sumElec > 0)
                            {
                                model.PorcAsistentes = ((double)sumAsist / sumElec * 100).ToString("0.000") + " %";
                                model.PorcAusentes = ((double)sumAus / sumElec * 100).ToString("0.000") + " %";
                            }
                            else
                            {
                                model.PorcAsistentes = "0.000 %";
                                model.PorcAusentes = "0.000 %";
                            }
                        }
                    }
                    else
                    {
                        using (var cmd = new SqlCommand("SELECT * FROM vTotalVotos", cn))
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                model.TotalAsistentes = dr.GetInt32(0);
                                model.PorcAsistentes = dr.IsDBNull(1) ? "0.000 %" : dr.GetString(1);
                                model.TotalAusentes = dr.GetInt32(2);
                                model.PorcAusentes = dr.IsDBNull(3) ? "0.000 %" : dr.GetString(3);
                                model.ElectoresHabiles = dr.GetInt32(4);
                            }
                        }

                        using (var cmd = new SqlCommand("usp_getVotos", cn) { CommandType = CommandType.StoredProcedure })
                        {
                            cmd.Parameters.AddWithValue("@inicio", 1);
                            cmd.Parameters.AddWithValue("@fin", 25);
                            using var dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                model.ListaDepartamentos.Add(new DatoResultado
                                {
                                    Columna1 = dr["DPD"]?.ToString() ?? "",
                                    Columna2 = dr["TV"]?.ToString() ?? "0",
                                    Columna3 = dr["PTV"]?.ToString() ?? "0.000 %",
                                    Columna4 = dr["TA"]?.ToString() ?? "0",
                                    Columna5 = dr["PTA"]?.ToString() ?? "0.000 %",
                                    Columna6 = dr["EH"]?.ToString() ?? "0"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error en ParticipacionDetalle: " + ex.Message;
            }

            return View("ParticipacionDetalle", model);
        }

        public IActionResult ActasNumero(string nroMesa = "")
        {
            var model = new ActaDetalle { Existe = false };
            if (!string.IsNullOrEmpty(nroMesa))
            {
                model = GetActaDetalle(nroMesa);
            }
            return View(model);
        }

        public IActionResult ActasUbigeo(
            string ambito = "P",
            string depa = "",
            string prov = "",
            string dist = "",
            string local = "",
            string mesa = "")
        {
            var model = new ActaUbigeoViewModel
            {
                AmbitoSeleccionado = ambito,
                DepartamentoSeleccionado = depa,
                ProvinciaSeleccionado = prov,
                DistritoSeleccionado = dist,
                LocalSeleccionado = local,
                MesaSeleccionada = mesa
            };

            try
            {
                using var cn = new SqlConnection(_con);
                cn.Open();

                using (var cmd = new SqlCommand("usp_getDepartamentos", cn) { CommandType = CommandType.StoredProcedure })
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int id = dr.GetInt32(0);
                        string nombre = dr.GetString(1).Trim();
                        if ((ambito == "P" && id <= 25) || (ambito == "E" && id > 25))
                        {
                            model.Departamentos.Add(new UbigeoSelectListItem { Value = id.ToString(), Text = nombre });
                        }
                    }
                }

                if (!string.IsNullOrEmpty(depa) && int.TryParse(depa, out int idDepa))
                {
                    using (var cmd = new SqlCommand("usp_getProvincias", cn) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("@idDepartamento", idDepa);
                        using var dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            model.Provincias.Add(new UbigeoSelectListItem
                            {
                                Value = dr.GetInt32(0).ToString(),
                                Text = dr.GetString(1).Trim()
                            });
                        }
                    }
                }

                if (!string.IsNullOrEmpty(prov) && int.TryParse(prov, out int idProv))
                {
                    using (var cmd = new SqlCommand("usp_getDistritos", cn) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("@idProvincia", idProv);
                        using var dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            model.Distritos.Add(new UbigeoSelectListItem
                            {
                                Value = dr.GetInt32(0).ToString(),
                                Text = dr.GetString(1).Trim()
                            });
                        }
                    }
                }

                if (!string.IsNullOrEmpty(dist) && int.TryParse(dist, out int idDist))
                {
                    using (var cmd = new SqlCommand("usp_getLocalesVotacion", cn) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("@idDistrito", idDist);
                        using var dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            model.Locales.Add(new UbigeoSelectListItem
                            {
                                Value = dr.GetInt32(0).ToString(),
                                Text = dr.GetString(1).Trim()
                            });
                        }
                    }
                }

                if (!string.IsNullOrEmpty(local) && int.TryParse(local, out int idLocal))
                {
                    using (var cmd = new SqlCommand("usp_getGruposVotacion", cn) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("@idLocalVotacion", idLocal);
                        using var dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            string mesaId = dr.GetString(0);
                            model.Mesas.Add(new UbigeoSelectListItem { Value = mesaId, Text = mesaId });
                        }
                    }
                }

                if (!string.IsNullOrEmpty(mesa))
                {
                    model.ActaDetalle = GetActaDetalle(mesa);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar ubigeo: " + ex.Message;
            }

            return View(model);
        }

        private ActaDetalle GetActaDetalle(string idGrupoVotacion)
        {
            var acta = new ActaDetalle { Existe = false };
            try
            {
                using var cn = new SqlConnection(_con);
                cn.Open();
                using var cmd = new SqlCommand("usp_getGrupoVotacion", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@idGrupoVotacion", idGrupoVotacion);
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    acta.Existe = true;
                    acta.Departamento = dr["Departamento"]?.ToString() ?? "";
                    acta.Provincia = dr["Provincia"]?.ToString() ?? "";
                    acta.Distrito = dr["Distrito"]?.ToString() ?? "";
                    acta.LocalVotacion = dr["RazonSocial"]?.ToString() ?? "";
                    acta.Direccion = dr["Direccion"]?.ToString() ?? "";
                    acta.MesaNro = dr["idGrupoVotacion"]?.ToString() ?? "";
                    acta.Copia = dr["nCopia"]?.ToString() ?? "";
                    acta.EstadoActa = dr.GetInt32(dr.GetOrdinal("idEstadoActa")) == 1
                        ? "ACTA ELECTORAL NORMAL"
                        : "ACTA ELECTORAL RESUELTA";
                    acta.ElectoresHabiles = dr.GetInt32(dr.GetOrdinal("ElectoresHabiles"));
                    acta.TotalVotantes = dr.GetInt32(dr.GetOrdinal("TotalVotantes"));
                    acta.P1 = dr.GetInt32(dr.GetOrdinal("P1"));
                    acta.P2 = dr.GetInt32(dr.GetOrdinal("P2"));
                    acta.Blancos = dr.GetInt32(dr.GetOrdinal("VotosBlancos"));
                    acta.Nulos = dr.GetInt32(dr.GetOrdinal("VotosNulos"));
                    acta.Impugnados = dr.GetInt32(dr.GetOrdinal("VotosImpugnados"));
                }
            }
            catch { }
            return acta;
        }
    }
}