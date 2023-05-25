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
    public class Armario1Controller : ControllerBase
    {
        private readonly ILogger<Armario1Controller> _logger;
        private readonly string _connectionString;


        public Armario1Controller(ILogger<Armario1Controller> logger)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false);
            IConfigurationRoot configuration = builder.Build();
            _connectionString = configuration.GetValue<string>("ConnectionStrings:SqlConnection");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ObterDados()
        {
            var dados = new List<Armario1>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand("select a1.*, m.MODELO from ARMARIO_1 a1 left outer join Maquinas m on (a1.serial = m.serial)", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            dados.Add(new Armario1
                            {
                                Serial = leitor["SERIAL"].ToString(),
                                Status = leitor["STATUS"].ToString(),
                                Situacao = leitor["SITUACAO"].ToString(),
                                Local = leitor["LOCAL"].ToString(),
                                Operadora = leitor["OPERADORA"].ToString(),
                                MaquinaPropriaDoCliente = leitor["MaquinaPropriaDoCliente"].ToString(),
                                Modelo = leitor["MODELO"].ToString()
                            });
                        }
                    }
                    return Ok(dados);
                }
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> MoverParaDefeito([FromBody] MoverParaDefeito request)
        {
            try
            {
                var sqlQuery = $@"INSERT INTO DEFEITOS(SERIAL, CAIXA, Motivo, DATA)
                                SELECT SERIAL, '{request.caixa}', '{request.motivo}',GETDATE() FROM ARMARIO_1 WHERE SERIAL = '{request.serial}'
                                INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA,  MOTIVO, DATA, CNPF, DataAlteracao)
                                SELECT SERIAL, 'ARMARIO_1', 'DEFEITOS', STATUS, SITUACAO, LOCAL, OPERADORA, '', MaquinaPropriaDoCliente, '', '','', '', GETDATE() FROM ARMARIO_1
                                where SERIAL = '{request.serial}'
                                DELETE FROM ARMARIO_1 WHERE SERIAL = '{request.serial}';";
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

        [HttpPost]
        public async Task<IActionResult> MoverParaCliente([FromBody] MoverParaCliente request)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();

                    using (var comando = new SqlCommand($@"INSERT INTO MaquinasNosClientes(SERIAL, CNPF, EMPRESA, DATA)
                                                          SELECT SERIAL, '{request.CNPF}', '{request.empresa}',GETDATE() FROM ARMARIO_1 WHERE SERIAL = '{request.serial}'
                                                          INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, DATA, CNPF, DataAlteracao)
                                                          SELECT SERIAL, 'ARMARIO_1', 'MaquinasNosClientes', STATUS, SITUACAO, LOCAL, OPERADORA, '', MaquinaPropriaDoCliente, '', '', '', GETDATE() FROM ARMARIO_1
                                                          WHERE SERIAL = '{request.serial}'
                                                          DELETE FROM ARMARIO_1 WHERE SERIAL = '{request.serial}';", conexao)
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
                                                         ARMARIO_1 a
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

        [HttpPost("[action]/{serial}/{campo}")]
        public async Task<IActionResult> AlterarCampo(string serial, string campo, string? valor)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();
                    using (var comando = new SqlCommand($@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, DATA, CNPF, DataAlteracao)
                                                           SELECT SERIAL, 'ARMARIO_1', 'ARMARIO_1', STATUS, SITUACAO, LOCAL, OPERADORA, '', MaquinaPropriaDoCliente, '', '', '', GETDATE() FROM ARMARIO_1
                                                           WHERE SERIAL = '{serial}'
                                                           UPDATE ARMARIO_1 set {campo} = '{valor}' where SERIAL = '{serial}';", conexao))
                    {
                        await comando.ExecuteNonQueryAsync();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao alterar o serial {serial}");
                return StatusCode(500);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> MoverParaEstoqueEstrangeiro([FromBody] MoverParaEstoqueExterno request)
        {
            try
            {
                using(var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();
                    using (var comando = new SqlCommand($@"INSERT INTO Historico(SERIAL, ORIGEM, DESTINO, STATUS, SITUACAO, LOCAL, OPERADORA, DataRetirada, MaquinaPropriaDoCliente, CAIXA, DATA, CNPF, DataAlteracao)
                                                           SELECT SERIAL, 'ARMARIO_1', 'EstoqueEstrangeiro', STATUS, SITUACAO, LOCAL, OPERADORA, '', MaquinaPropriaDoCliente, '', '', '', GETDATE() FROM ARMARIO_1
                                                           WHERE SERIAL = '{request.serial}'
                                                           INSERT INTO EstoqueEstrangeiro(SERIAL, LOCAL)
                                                           SELECT SERIAL, '{request.local}' from ARMARIO_1 WHERE SERIAL = '{request.serial}'
                                                           DELETE FROM ARMARIO_1 WHERE SERIAL = '{request.serial}';", conexao))
                    {
                        await comando.ExecuteNonQueryAsync();
                    }
                }
                return Ok();
            }
            catch( Exception ex )
            {
                _logger.LogError(ex, $"Erro ao mover {request.serial} para EstoqueEstrangeiro");
                return StatusCode(500);
            }
        }
    }
}

