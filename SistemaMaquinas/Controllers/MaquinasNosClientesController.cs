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
    public class MaquinasNosClientesController : ControllerBase
    {
        private readonly ILogger<MaquinasNosClientesController> _logger;
        private readonly string _connectionString;

        public MaquinasNosClientesController(ILogger<MaquinasNosClientesController> logger)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false);
            IConfigurationRoot configuration = builder.Build();
            _connectionString = configuration.GetValue<string>("ConnectionStrings:SqlConnection");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ObterDados()
        {
            var dados = new List<MaquinasNosClientes>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand("select mC.*, m.MODELO from MaquinasNosClientes mC left outer join Maquinas m on (mC.serial = m.serial)", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            dados.Add(new MaquinasNosClientes
                            {
                                Serial = leitor["SERIAL"].ToString(),
                                Modelo = leitor["MODELO"].ToString(),
                                CNPF = leitor["CNPF"].ToString(),
                                Data = leitor["DATA"].ToString(),
                                Empresa = leitor["EMPRESA"].ToString()
                            });
                        }
                    }
                    return Ok(dados);
                }
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> MoverParaArmario2([FromBody] MoverParaArmario2 request)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();

                    using (var comando = new SqlCommand($@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, EMPRESA, DATA, CNPF, DataAlteracao)
                                                           SELECT SERIAL, 'MaquinasNosClientes', 'ARMARIO_2', '', '', '', '', '', '', '', EMPRESA, DATA, CNPF, GETDATE() FROM MaquinasNosClientes
                                                           WHERE SERIAL = '{request.Serial}'
                                                           INSERT INTO ARMARIO_2(SERIAL, STATUS, SITUACAO, LOCAL)
                                                           SELECT SERIAL, 'LOGISTICA REVERSA', '{request.Situacao}', '{request.Local}' FROM MaquinasNosClientes WHERE SERIAL = '{request.Serial}'                                                            
                                                           DELETE FROM MaquinasNosClientes
                                                           WHERE SERIAL = '{request.Serial}';", conexao)
                                                        )
                    {
                        await comando.ExecuteNonQueryAsync();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao mover o serial {request.Serial} para a tabela ARMARIO_2");
                return StatusCode(500);
            }
        }


        [HttpPost("[action]")]
        public async Task<IActionResult> MoverParaDefeitoExterior(MoverParaDefeitoExterior request)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();

                    using (var comando = new SqlCommand($@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, EMPRESA, DATA, CNPF, DataAlteracao)
                                                           SELECT SERIAL, 'MaquinasNosClientes', 'DefeitoExterior', '', '', '', '', '', '', '', EMPRESA, DATA, CNPF, GETDATE() FROM MaquinasNosClientes
                                                           WHERE SERIAL = '{request.Serial}'
                                                           INSERT INTO DefeitoExterior(SERIAL, LOCAL)
                                                           SELECT SERIAL, '{request.Local}' FROM MaquinasNosClientes WHERE SERIAL = '{request.Serial}'                                                            
                                                           DELETE FROM MaquinasNosClientes
                                                           WHERE SERIAL = '{request.Serial}';", conexao)
                                                        )
                    {
                        await comando.ExecuteNonQueryAsync();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao mover o serial {request.Serial} para a tabela Defeito Exterior");
                return StatusCode(500);
            }
        }

        [HttpGet("[action]/{dataInicial}/{dataFinal}")]
        public async Task<IActionResult> Modelos(string dataInicial, string dataFinal)
        {
            var modelos = new List<Modelos>();
            
            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand($@"SELECT
                                                          SUM(CASE WHEN m.MODELO = 'D3 - PRO 1' THEN 1 ELSE 0 END) AS 'D3 - PRO 1',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - PRO 2' THEN 1 ELSE 0 END) AS 'D3 - PRO 2',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - PRO REFURBISHED' THEN 1 ELSE 0 END) AS 'D3 - PRO REFURBISHED',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - SMART' THEN 1 ELSE 0 END) AS 'D3 - SMART',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - X' THEN 1 ELSE 0 END) AS 'D3 - X',
                                                          COUNT(a.SERIAL) AS Total
                                                      FROM
                                                          MaquinasNosClientes a
                                                          LEFT OUTER JOIN Maquinas m ON a.SERIAL = m.serial
                                                          WHERE DATA BETWEEN '{dataInicial}' and '{dataFinal}'", conexao))
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

        [HttpGet("[action]")]
        public async Task<IActionResult> ModeloTotal()
        {
            var modelos = new List<Modelos>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand($@"SELECT
                                                          SUM(CASE WHEN m.MODELO = 'D3 - PRO 1' THEN 1 ELSE 0 END) AS 'D3 - PRO 1',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - PRO 2' THEN 1 ELSE 0 END) AS 'D3 - PRO 2',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - PRO REFURBISHED' THEN 1 ELSE 0 END) AS 'D3 - PRO REFURBISHED',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - SMART' THEN 1 ELSE 0 END) AS 'D3 - SMART',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - X' THEN 1 ELSE 0 END) AS 'D3 - X',
                                                          COUNT(a.SERIAL) AS Total
                                                      FROM
                                                          MaquinasNosClientes a
                                                          LEFT OUTER JOIN Maquinas m ON a.SERIAL = m.serial", conexao))
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
