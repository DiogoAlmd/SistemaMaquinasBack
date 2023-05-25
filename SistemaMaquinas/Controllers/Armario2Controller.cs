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
    public class Armario2Controller : ControllerBase
    {
        private readonly ILogger<Armario2Controller> _logger;
        private readonly string _connectionString;

        public Armario2Controller(ILogger<Armario2Controller> logger)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false);
            IConfigurationRoot configuration = builder.Build();
            _connectionString = configuration.GetValue<string>("ConnectionStrings:SqlConnection");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ObterDados()
        {
            var dados = new List<Armario2>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand("select a2.*, m.MODELO from ARMARIO_2 a2 left outer join Maquinas m on (a2.serial = m.serial)", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            dados.Add(new Armario2
                            {
                                Serial = leitor["SERIAL"].ToString(),
                                Modelo = leitor["MODELO"].ToString(),
                                Status = leitor["STATUS"].ToString(),
                                Situacao = leitor["SITUACAO"].ToString(),
                                Local = leitor["LOCAL"].ToString(),
                            });
                        }
                    }
                    return Ok(dados);
                }
            }
        }

        [HttpPost("[action]/{serial}/{operadora}")]
        public async Task<IActionResult> MoverParaArmario1(string serial, string operadora)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();

                    using (var comando = new SqlCommand($@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, DATA, CNPF, DataAlteracao)
                                                           SELECT SERIAL, 'ARMARIO_2', 'ARMARIO_1', STATUS, SITUACAO, LOCAL, '', '', '', '', '', '', GETDATE() FROM ARMARIO_2
                                                           WHERE SERIAL = '{serial}'
                                                           INSERT INTO ARMARIO_1(SERIAL, STATUS, SITUACAO, LOCAL, OPERADORA)
                                                           SELECT SERIAL, 'ATIVAÇÃO', 'TRATADO', LOCAL, '{operadora}' FROM ARMARIO_2 WHERE SERIAL = '{serial}'                                                            
                                                           DELETE FROM ARMARIO_2
                                                           WHERE SERIAL = '{serial}';", conexao)
                                                        )
                    {
                        await comando.ExecuteNonQueryAsync();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao mover o serial {serial} para a tabela ARMARIO_1");
                return StatusCode(500);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> MoverParaDefeito([FromBody] MoverParaDefeito request)
        {
            try
            {
                var sqlQuery = $@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, Motivo, DATA, CNPF, DataAlteracao)
                                  SELECT SERIAL, 'ARMARIO_2', 'DEFEITOS', STATUS, SITUACAO, LOCAL, '', '', '', '', '', '', '', GETDATE() FROM ARMARIO_2
                                  WHERE SERIAL = '{request.serial}'
                                  INSERT INTO DEFEITOS(SERIAL, CAIXA, Motivo, DATA)
                                  SELECT SERIAL, '{request.caixa}', '{request.motivo}', GETDATE() FROM ARMARIO_2 WHERE SERIAL = '{request.serial}'
                                  DELETE FROM ARMARIO_2 WHERE SERIAL = '{request.serial}';";
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

                using (var comando = new SqlCommand(@" SELECT
                                                          SUM(CASE WHEN m.MODELO = 'D3 - PRO 1' THEN 1 ELSE 0 END) AS 'D3 - PRO 1',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - PRO 2' THEN 1 ELSE 0 END) AS 'D3 - PRO 2',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - PRO REFURBISHED' THEN 1 ELSE 0 END) AS 'D3 - PRO REFURBISHED',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - SMART' THEN 1 ELSE 0 END) AS 'D3 - SMART',
                                                          SUM(CASE WHEN m.MODELO = 'D3 - X' THEN 1 ELSE 0 END) AS 'D3 - X',
                                                          COUNT(a.SERIAL) AS Total
                                                        FROM
                                                          ARMARIO_2 a
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

