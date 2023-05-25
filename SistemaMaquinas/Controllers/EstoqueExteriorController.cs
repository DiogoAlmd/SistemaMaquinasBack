using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaMaquinas.Models;
using SistemaMaquinas.Classes;
using SistemaMaquinas.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace SistemaMaquinas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EstoqueExteriorController : ControllerBase
    {
        private readonly ILogger<EstoqueExteriorController> _logger;
        private readonly string _connectionString;


        public EstoqueExteriorController(ILogger<EstoqueExteriorController> logger)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false);
            IConfigurationRoot configuration = builder.Build();
            _connectionString = configuration.GetValue<string>("ConnectionStrings:SqlConnection");
            _logger = logger;
        }


        [HttpGet]
        public async Task<IActionResult> ObterDados()
        {
            var dados = new List<EstoqueExterior>();
            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand("select eE.*, m.MODELO from EstoqueEstrangeiro eE left outer join Maquinas m on (eE.serial = m.serial)", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            dados.Add(new EstoqueExterior
                            {
                                Serial = leitor["SERIAL"].ToString(),
                                Modelo = leitor["MODELO"].ToString(),
                                Local = leitor["LOCAL"].ToString()
                            });
                        }
                    }
                    return Ok(dados);
                }
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> MoverParaDefeitoExterior([FromBody] MoverParaDefeitoExterior request)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();

                    using (var comando = new SqlCommand($@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, LOCAL, DataAlteracao)
                                                           SELECT SERIAL, 'EstoqueEstrangeiro', 'DefeitoExterior', LOCAL, GETDATE() FROM EstoqueEstrangeiro
                                                           WHERE SERIAL = '{request.Serial}'
                                                           INSERT INTO DefeitoExterior(SERIAL, LOCAL)
                                                           SELECT SERIAL, '{request.Local}' FROM EstoqueEstrangeiro WHERE SERIAL = '{request.Serial}'                                                            
                                                           DELETE FROM EstoqueEstrangeiro
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

        [HttpPost]
        public async Task<IActionResult> MoverParaCliente([FromBody] MoverParaCliente request)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();

                    using (var comando = new SqlCommand($@"INSERT INTO MaquinasNosClientes(SERIAL, CNPF, EMPRESA, DATA)
                                                          SELECT SERIAL, '{request.CNPF}', '{request.empresa}', GETDATE() FROM EstoqueEstrangeiro WHERE SERIAL = '{request.serial}'
                                                          INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, DATA, CNPF, DataAlteracao)
                                                          SELECT SERIAL, 'EstoqueEstrangeiro', 'MaquinasNosClientes', '', '', LOCAL, '', '', '', '', '', '', GETDATE() FROM EstoqueEstrangeiro
                                                          WHERE SERIAL = '{request.serial}'
                                                          DELETE FROM EstoqueEstrangeiro WHERE SERIAL = '{request.serial}';", conexao)
                                                        )
                    {
                        await comando.ExecuteNonQueryAsync();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao mover o serial {request.serial} para a tabela MaquinasNoCliente");
                return StatusCode(500);
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

                    using (var comando = new SqlCommand($@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, DATA, CNPF, DataAlteracao)
                                                           SELECT SERIAL, 'EstoqueEstrangeiro', 'ARMARIO_2', '', '', LOCAL, '', '', '', '', '', '', GETDATE() FROM EstoqueEstrangeiro
                                                           WHERE SERIAL = '{request.Serial}'
                                                           INSERT INTO ARMARIO_2(SERIAL, STATUS, SITUACAO, LOCAL)
                                                           SELECT SERIAL, 'LOGISTICA REVERSSA', '{request.Situacao}', '{request.Local}' FROM EstoqueEstrangeiro WHERE SERIAL = '{request.Serial}'                                                            
                                                           DELETE FROM EstoqueEstrangeiro
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

        [HttpGet("[action]")]
        public async Task<IActionResult> Modelos()
        {
            var CidadesEstrangeiras = new List<CidadesEstrangeiras>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand(@"SELECT
                                                         SUM(CASE WHEN LOCAL = 'RIO PRETO' THEN 1 ELSE 0 END) AS 'RIO PRETO',
                                                         SUM(CASE WHEN LOCAL= 'CAMPO GRANDE' THEN 1 ELSE 0 END) AS 'CAMPO GRANDE',
                                                         SUM(CASE WHEN LOCAL = 'RIO DE JANEIRO' THEN 1 ELSE 0 END) AS 'RIO DE JANEIRO',
                                                         SUM(CASE WHEN LOCAL = 'CX PATRICK' THEN 1 ELSE 0 END) AS 'CX PATRICK',
                                                         SUM(CASE WHEN LOCAL = 'DECIO' THEN 1 ELSE 0 END) AS 'DECIO',
                                                         SUM(CASE WHEN LOCAL = 'EVENTO 3A' THEN 1 ELSE 0 END) AS 'EVENTO 3A',
                                                         COUNT(SERIAL) AS Total
                                                       FROM EstoqueEstrangeiro", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            CidadesEstrangeiras.Add(new CidadesEstrangeiras
                            {
                                RioPreto = leitor["RIO PRETO"].ToString(),
                                CampoGrande = leitor["CAMPO GRANDE"].ToString(),
                                RioDeJaneiro = leitor["RIO DE JANEIRO"].ToString(),
                                CxPatrick = leitor["CX PATRICK"].ToString(),
                                Decio = leitor["DECIO"].ToString(),
                                Evento3A = leitor["EVENTO 3A"].ToString(),
                                Total = leitor["Total"].ToString()
                            });
                        }
                    }
                    return Ok(CidadesEstrangeiras);
                }
            }
        }
    }
}
