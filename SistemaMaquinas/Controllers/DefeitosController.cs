using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaMaquinas.Classes;
using SistemaMaquinas.Models;

namespace SistemaMaquinas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DefeitosController : ControllerBase
    {
        private readonly ILogger<DefeitosController> _logger;
        private readonly string _connectionString;

        public DefeitosController(ILogger<DefeitosController> logger)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false);
            IConfigurationRoot configuration = builder.Build();
            _connectionString = configuration.GetValue<string>("ConnectionStrings:SqlConnection");
            _logger = logger;
        }


        [HttpGet]
        public async Task<IActionResult> ObterDados()
        {
            var dados = new List<Defeitos>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand("select d.*, m.modelo from DEFEITOS d left outer join Maquinas m on (d.serial = m.serial)", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            dados.Add(new Defeitos
                            {
                                Serial = leitor["SERIAL"].ToString(),
                                Modelo = leitor["MODELO"].ToString(),
                                Caixa = leitor["CAIXA"].ToString(),
                                Data = leitor["DATA"].ToString().Replace("00:00:00", ""),
                                Motivo = leitor["MOTIVO"].ToString()
                            });
                        }
                    }
                    return Ok(dados);
                }
            }
        }

        [HttpPut("[action]")]
        public async Task<IActionResult> AlterarMotivo(AlterarMotivoDefeitos request)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();

                    using (var comando = new SqlCommand($@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, Motivo, CAIXA, DATA, CNPF, DataAlteracao)
                                                           SELECT SERIAL, 'DEFEITOS', 'DEFEITOS', '', '', '', '', '', '', Motivo, CAIXA, DATA, '', GETDATE() FROM DEFEITOS
                                                           WHERE SERIAL = '{request.Serial}'
                                                           UPDATE DEFEITOS
                                                           SET MOTIVO = '{request.NovoMotivo}'
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
                _logger.LogError(ex, $"Erro ao alterar o motivo do serial {request.Serial} da tabela DEFEITOS");
                return StatusCode(500);
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Motivos()
        {
            var motivodefeito = new List<MotivoDefeito>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand(@"SELECT
                                                         SUM(CASE WHEN MOTIVO = 'Ped Tampered' THEN 1 ELSE 0 END) AS 'Ped Tampered',
                                                         SUM(CASE WHEN MOTIVO = 'Erro na leitura do cartão' THEN 1 ELSE 0 END) AS 'Erro na leitura do cartão',
                                                         SUM(CASE WHEN MOTIVO = 'Touch' THEN 1 ELSE 0 END) AS 'Touch',
                                                         SUM(CASE WHEN MOTIVO = 'Conector com defeito' THEN 1 ELSE 0 END) AS 'Conector com defeito',
                                                         SUM(CASE WHEN MOTIVO = 'Conectividade de chips' THEN 1 ELSE 0 END) AS 'Conectividade de chips',
                                                         SUM(CASE WHEN MOTIVO = 'Estética' THEN 1 ELSE 0 END) AS 'Estética',
                                                         SUM(CASE WHEN MOTIVO = 'Defeito de Impressão' THEN 1 ELSE 0 END) AS 'Defeito de Impressão',
                                                         COUNT(SERIAL) AS Total
                                                       FROM DEFEITOS", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            motivodefeito.Add(new MotivoDefeito
                            {
                                PedTampered = leitor["Ped Tampered"].ToString(),
                                ErroNaLeituraDoCartao = leitor["Erro na leitura do cartão"].ToString(),
                                Touch = leitor["Touch"].ToString(),
                                ConectorComDefeito = leitor["Conector com defeito"].ToString(),
                                ConectividadeDeChips = leitor["Conectividade de chips"].ToString(),
                                Estetica = leitor["Estética"].ToString(),
                                DefeitoDeImpressao = leitor["Defeito de Impressão"].ToString(),
                                Total = leitor["Total"].ToString()
                            });
                        }
                    }
                    return Ok(motivodefeito);
                }
            }
        }
    }
}
