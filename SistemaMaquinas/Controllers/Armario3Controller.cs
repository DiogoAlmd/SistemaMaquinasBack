using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaMaquinas.Models;
using SistemaMaquinas.Classes;
using SistemaMaquinas.Repositories;
using Microsoft.AspNetCore.Authorization;
using Azure.Core;

namespace SistemaMaquinas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class Armario3Controller : ControllerBase
    {
        private readonly ILogger<Armario2Controller> _logger;
        private readonly string _connectionString;

        public Armario3Controller(ILogger<Armario2Controller> logger)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false);
            IConfigurationRoot configuration = builder.Build();
            _connectionString = configuration.GetValue<string>("ConnectionStrings:SqlConnection");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ObterDados()
        {
            var dados = new List<Armario3>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand("select a3.*, m.modelo from ARMARIO_3 a3 left outer join Maquinas m on (a3.serial = m.serial)", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            dados.Add(new Armario3
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

        [HttpPost("[action]/{serial}/{usuario}")]
        public async Task<IActionResult> MoverParaArmario1(string serial, string usuario)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();

                    using (var comando = new SqlCommand($@"DECLARE @usuario int
                                                           SET @usuario = (SELECT idUsuario FROM users WHERE loginUsuario = '{usuario}')
                                                           INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, USUARIO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, DATA, CNPF, DataAlteracao)
                                                           SELECT SERIAL, 'ARMARIO_3', 'ARMARIO_1', @usuario, STATUS, SITUACAO, LOCAL, '', '', '', '', '', '', GETDATE() FROM ARMARIO_3
                                                           WHERE SERIAL = '{serial}'
                                                           INSERT INTO ARMARIO_1(SERIAL, STATUS, SITUACAO, LOCAL)
                                                           SELECT SERIAL, 'ATIVAÇÃO', 'TRATADO', LOCAL
                                                           FROM ARMARIO_3
                                                           WHERE SERIAL = '{serial}'
                                                           DELETE FROM ARMARIO_3
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
                var sqlQuery = $@"DECLARE @usuario int
                                  SET @usuario = (SELECT idUsuario FROM users WHERE loginUsuario = '{request.usuario}')
                                  INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, USUARIO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, Motivo, CAIXA, DATA, CNPF, DataAlteracao)
                                  SELECT SERIAL, 'ARMARIO_3', 'DEFEITOS', @usuario, STATUS, SITUACAO, LOCAL, '','', '', '', '', '', '', GETDATE() FROM ARMARIO_3
                                  WHERE SERIAL = '{request.serial}'
                                  INSERT INTO DEFEITOS(SERIAL, CAIXA, Motivo, DATA)
                                  SELECT SERIAL, '{request.caixa}', '{request.motivo}', GETDATE() FROM ARMARIO_3 WHERE SERIAL = '{request.serial}'
                                  DELETE FROM ARMARIO_3 WHERE SERIAL = '{request.serial}';";
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
                                                        SUM(CASE WHEN m.MODELO = 'D3 - PRO 2' THEN 1 ELSE 0 END) AS 'D3 - PRO 2',
                                                        SUM(CASE WHEN m.MODELO = 'D3 - PRO REFURBISHED' THEN 1 ELSE 0 END) AS 'D3 - PRO REFURBISHED',
                                                        COUNT(a.SERIAL) AS Total
                                                      FROM
                                                        ARMARIO_3 a
                                                        LEFT OUTER JOIN Maquinas m ON a.SERIAL = m.serial;", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            modelos.Add(new Modelos
                            {
                                d3Pro2 = leitor["D3 - PRO 2"].ToString(),
                                d3ProRefurbished = leitor["D3 - PRO REFURBISHED"].ToString(),
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
