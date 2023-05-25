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
    public class EstoqueABController : ControllerBase
    {
        private readonly ILogger<EstoqueABController> _logger;
        private readonly string _connectionString;

        public EstoqueABController(ILogger<EstoqueABController> logger)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false);
            IConfigurationRoot configuration = builder.Build();
            _connectionString = configuration.GetValue<string>("ConnectionStrings:SqlConnection");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ObterDados()
        {
            var dados = new List<EstoqueAB>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand("select ab.*, m.modelo from ESTOQUE_AB ab left outer join Maquinas m on (ab.serial = m.serial)", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            dados.Add(new EstoqueAB
                            {
                                Serial = leitor["SERIAL"].ToString(),
                                Modelo = leitor["MODELO"].ToString(),
                                Status = leitor["STATUS"].ToString(),
                                Situacao = leitor["SITUACAO"].ToString(),
                                Local = leitor["LOCAL"].ToString()
                            });
                        }
                    }
                    return Ok(dados);
                }
            }
        }
        [HttpPost("[action]/{serial}/{novaTabela}")]
        public async Task<IActionResult> MoverParaNovaTabela(string serial, string novaTabela)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();

                    switch (novaTabela)
                    {
                        case "ARMARIO_1":
                            using (var comando = new SqlCommand($@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, DATA, CNPF, DataAlteracao)
                                                                   SELECT SERIAL, 'ESTOQUE_AB', 'ARMARIO_1', STATUS, SITUACAO, LOCAL, '', '', '', '', '','', GETDATE() FROM ESTOQUE_AB WHERE SERIAL = '{serial}'
                                                                   INSERT INTO ARMARIO_1(SERIAL, STATUS, SITUACAO, LOCAL)
                                                                   SELECT SERIAL, 'ATIVAÇÃO', 'TRATADO', 'D3'
                                                                   FROM ESTOQUE_AB
                                                                   WHERE SERIAL = '{serial}'
                                                                   DELETE FROM ESTOQUE_AB
                                                                   WHERE SERIAL = '{serial}';", conexao)
                                                                )
                            {
                                await comando.ExecuteNonQueryAsync();
                            }
                            break;

                        case "ARMARIO_3":
                            using (var comando = new SqlCommand($@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, DATA, CNPF, DataAlteracao)
                                                                   SELECT SERIAL, 'ESTOQUE_AB', 'ARMARIO_3', STATUS, SITUACAO, LOCAL, '', '', '', '', '','', GETDATE() FROM ESTOQUE_AB WHERE SERIAL = '{serial}'
                                                                   INSERT INTO ARMARIO_3(SERIAL, STATUS, SITUACAO, LOCAL)
                                                                   SELECT SERIAL, 'BRUTA', SITUACAO, 'D3'
                                                                   FROM ESTOQUE_AB
                                                                   WHERE SERIAL = '{serial}'
                                                                   DELETE FROM ESTOQUE_AB
                                                                   WHERE SERIAL = '{serial}';", conexao)
                                                                )
                            {
                                await comando.ExecuteNonQueryAsync();
                            }
                            break;
                        default: return StatusCode(404);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao mover o serial {serial} para a tabela {novaTabela}");
                return StatusCode(500);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> MoverParaDefeito([FromBody] MoverParaDefeito request)
        {
            try
            {
                var sqlQuery = $@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, Motivo, CAIXA, DATA, CNPF, DataAlteracao)
                                  SELECT SERIAL, 'ESTOQUE_AB', 'DEFEITOS', STATUS, SITUACAO, LOCAL, '', '', '', '', '', '', '', GETDATE() FROM ESTOQUE_AB
                                  WHERE SERIAL = '{request.serial}'
                                  INSERT INTO DEFEITOS(SERIAL, CAIXA, Motivo, DATA)
                                  SELECT SERIAL, '{request.caixa}', '{request.motivo}', GETDATE() FROM ESTOQUE_AB WHERE SERIAL = '{request.serial}'
                                  DELETE FROM ESTOQUE_AB WHERE SERIAL = '{request.serial}';";
                var repository = new DefeitosRepository(_connectionString, _logger, sqlQuery);
                await repository.MoverParaDefeito(request);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao mover o serial {request.serial} para a tabela DEFEITOS");
                return StatusCode(500);
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
                                                        SUM(CASE WHEN m.MODELO = 'D3 - SMART' THEN 1 ELSE 0 END) AS 'D3 - SMART',
                                                        COUNT(a.SERIAL) AS Total
                                                     FROM
                                                        ESTOQUE_AB a
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
                                d3Smart = leitor["D3 - SMART"].ToString(),
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
