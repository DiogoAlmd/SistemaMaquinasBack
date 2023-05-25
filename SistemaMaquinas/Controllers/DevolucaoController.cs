using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaMaquinas.Classes;
using SistemaMaquinas.Models;

namespace SistemaMaquinas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DevolucaoController : ControllerBase
    {
        private readonly ILogger<DevolucaoController> _logger;
        private readonly string _connectionString;

        public DevolucaoController(ILogger<DevolucaoController> logger)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false);
            IConfigurationRoot configuration = builder.Build();
            _connectionString = configuration.GetValue<string>("ConnectionStrings:SqlConnection");
            _logger = logger;
        }


        [HttpGet]
        public async Task<IActionResult> ObterDados()
        {
            var dados = new List<Devolucao>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand("select d.*, m.modelo from DEVOLUCAO d left outer join Maquinas m on (d.serial = m.serial)", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            dados.Add(new Devolucao
                            {
                                Serial = leitor["SERIAL"].ToString(),
                                Modelo = leitor["MODELO"].ToString(),
                                Caixa = leitor["CAIXA"].ToString(),
                                Data = leitor["DATA"].ToString()
                            });
                        }
                    }
                    return Ok(dados);
                }
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Modelos()
        {
            var modelos = new List<Modelos>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand(@"SELECT
                                                         SUM(CASE WHEN m.MODELO = 'D3 - PRO 1' THEN 1 ELSE 0 END) AS 'D3 - PRO 1',
                                                         SUM(CASE WHEN m.MODELO = 'D3 - PRO 2' THEN 1 ELSE 0 END) AS 'D3 - PRO 2',
                                                         SUM(CASE WHEN m.MODELO = 'D3 - PRO REFURBISHED' THEN 1 ELSE 0 END) AS 'D3 - PRO REFURBISHED',
                                                         SUM(CASE WHEN m.MODELO = 'D3 - SMART' THEN 1 ELSE 0 END) AS 'D3 - SMART',
                                                         SUM(CASE WHEN m.MODELO = 'D3 - X' THEN 1 ELSE 0 END) AS 'D3 - X',
                                                         COUNT(a.SERIAL) AS Total
                                                       FROM
                                                         DEVOLUCAO a
                                                         LEFT OUTER JOIN Maquinas m ON a.SERIAL = m.serial;", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            modelos.Add(new Modelos
                            {
                                d3Pro1 = leitor["D3 - PRO 1"].ToString(),
                                d3Pro2 = leitor["D3 - PRO 2"].ToString(),
                                d3ProRefurbished = leitor["D3 - PRO REFURBISHED"].ToString(),
                                d3Smart = leitor["D3 - SMART"].ToString(),
                                d3X = leitor["D3 - X"].ToString(),
                                Total = leitor["Total"].ToString()
                            });
                        }
                    }
                    return Ok(modelos);
                }
            }
        }
    }
}
