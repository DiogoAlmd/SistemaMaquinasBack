using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaMaquinas.Models;

namespace SistemaMaquinas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HistoricoController : ControllerBase
    {
        private readonly ILogger<HistoricoController> _logger;
        private readonly string _connectionString;

        public HistoricoController(ILogger<HistoricoController> logger)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false);
            IConfigurationRoot configuration = builder.Build();
            _connectionString = configuration.GetValue<string>("ConnectionStrings:SqlConnection");
            _logger = logger;
        }



        [HttpGet]
        public async Task<IActionResult> ObterDados()
        {
            var dados = new List<Historico>();

            using (var conexao = new SqlConnection(_connectionString))
            {
                await conexao.OpenAsync();

                using (var comando = new SqlCommand($@"Select h.id, h.SERIAL, h.ORIGEM, h.DESTINO, h.STATUS,
                                                       h.SITUACAO, h.LOCAL, h.OPERADORA, h.DataRetirada, h.MaquinaPropriaDoCliente, 
                                                       h.CAIXA, h.DATA, h.CNPF, h.DataAlteracao, h.MOTIVO, h.EMPRESA, u.loginUsuario AS USUARIO 
                                                       from Historico h left JOIN users u on h.USUARIO=u.idUsuario", conexao))
                {
                    using (var leitor = await comando.ExecuteReaderAsync())
                    {
                        while (await leitor.ReadAsync())
                        {
                            dados.Add(new Historico
                            {
                                Id = leitor["id"].ToString(),
                                Serial = leitor["SERIAL"].ToString(),
                                Origem = leitor["ORIGEM"].ToString(),
                                Destino= leitor["DESTINO"].ToString(),
                                Usuario = leitor["USUARIO"].ToString(),
                                Status = leitor["STATUS"].ToString(),
                                Situacao = leitor["SITUACAO"].ToString(),
                                Local = leitor["LOCAL"].ToString(),
                                Operadora = leitor["OPERADORA"].ToString(),
                                DataRetirada = leitor["DataRetirada"].ToString().Replace("00:00:00", ""),
                                MaquinaPropriaDoCliente = leitor["MaquinaPropriaDoCliente"].ToString(),
                                Caixa = leitor["CAIXA"].ToString(),
                                Motivo = leitor["MOTIVO"].ToString(),
                                Data = leitor["DATA"].ToString(),
                                CNPF = leitor["CNPF"].ToString(),
                                Empresa = leitor["EMPRESA"].ToString(),
                                DataAlteracao = leitor["DataAlteracao"].ToString()
                            });
                        }
                    }
                    return Ok(dados);
                }
            }
        }


        [HttpPost("[action]/{id}/{serial}/{origem}/{destino}")]
        public async Task<IActionResult> desfazer(int id, string serial, string origem, string destino)
        {
            try
            {
                using (var conexao = new SqlConnection(_connectionString))
                {
                    await conexao.OpenAsync();

                    switch (origem)
                    {
                        case "ARMARIO_1":
                            if(destino == "ARMARIO_1")
                            {
                                using (var comando = new SqlCommand($@"DELETE FROM ARMARIO_1 WHERE SERIAL = '{serial}'
                                                                       INSERT INTO ARMARIO_1(SERIAL, STATUS, SITUACAO, LOCAL, OPERADORA, MaquinaPropriaDoCliente)
                                                                       SELECT SERIAL, STATUS, SITUACAO, LOCAL, OPERADORA, MaquinaPropriaDoCliente FROM Historico WHERE id = '{id}' and SERIAL = '{serial}'
                                                                       DELETE FROM HISTORICO WHERE id = '{id}';", conexao)
                                                                    )
                                {
                                    await comando.ExecuteNonQueryAsync();
                                }
                                break;
                            }
                            else
                            {
                                using (var comando = new SqlCommand($@"DELETE FROM ARMARIO_1 WHERE SERIAL = '{serial}'
                                                                       INSERT INTO ARMARIO_1(SERIAL, STATUS, SITUACAO, LOCAL, OPERADORA, MaquinaPropriaDoCliente)
                                                                       SELECT SERIAL, STATUS, SITUACAO, LOCAL, OPERADORA, MaquinaPropriaDoCliente FROM Historico WHERE id = '{id}'
                                                                       DELETE FROM {destino} WHERE SERIAL = '{serial}'
                                                                       DELETE FROM HISTORICO WHERE SERIAL = '{serial}';", conexao)
                                                                    )
                                {
                                    await comando.ExecuteNonQueryAsync();
                                }
                                break;
                            }

                        case "ARMARIO_2":
                            using (var comando = new SqlCommand($@"INSERT INTO ARMARIO_2(SERIAL, STATUS, SITUACAO, LOCAL)
                                                                   SELECT SERIAL, STATUS, SITUACAO, LOCAL FROM Historico WHERE id = '{id}'
                                                                   DELETE FROM {destino} WHERE SERIAL = '{serial}'
                                                                   DELETE FROM HISTORICO WHERE SERIAL = '{serial}';", conexao)
                                                                )
                            {
                                await comando.ExecuteNonQueryAsync();
                            }
                            break;
                        case "ARMARIO_3":
                            using (var comando = new SqlCommand($@"INSERT INTO ARMARIO_3(SERIAL, STATUS, SITUACAO, LOCAL)
                                                                   SELECT SERIAL, STATUS, SITUACAO, LOCAL FROM Historico WHERE id = '{id}'
                                                                   DELETE FROM {destino} WHERE SERIAL = '{serial}'
                                                                   DELETE FROM HISTORICO WHERE SERIAL = '{serial}';", conexao)
                                                                )
                            {
                                await comando.ExecuteNonQueryAsync();
                            }
                            break;
                        case "ESTOQUE_AB":
                            using (var comando = new SqlCommand($@"INSERT INTO ESTOQUE_AB(SERIAL, STATUS, SITUACAO, LOCAL)
                                                                   SELECT SERIAL, STATUS, SITUACAO, LOCAL FROM Historico WHERE id = '{id}'
                                                                   DELETE FROM {destino} WHERE SERIAL = '{serial}'
                                                                   DELETE FROM HISTORICO WHERE SERIAL = '{serial}';", conexao)
                                                                )
                            {
                                await comando.ExecuteNonQueryAsync();
                            }
                            break;
                        case "MaquinasNosClientes":
                            using (var comando = new SqlCommand($@"INSERT INTO MaquinasNosClientes(SERIAL, CNPF, DATA, EMPRESA)
                                                                   SELECT SERIAL, CNPF, DATA, EMPRESA FROM Historico WHERE id = '{id}'
                                                                   DELETE FROM {destino} WHERE SERIAL = '{serial}'
                                                                   DELETE FROM HISTORICO WHERE SERIAL = '{serial}';", conexao)
                                                                )
                            {
                                await comando.ExecuteNonQueryAsync();
                            }
                            break;
                        case "DEFEITOS":
                            using (var comando = new SqlCommand($@"INSERT INTO DEFEITOS(SERIAL, CAIXA, DATA, MOTIVO)
                                                                   SELECT SERIAL, CAIXA, DATA, MOTIVO FROM Historico WHERE id = '{id}'
                                                                   DELETE FROM {destino} WHERE SERIAL = '{serial}'
                                                                   DELETE FROM HISTORICO WHERE SERIAL = '{serial}';", conexao)
                                                                )
                            {
                                await comando.ExecuteNonQueryAsync();
                            }
                            break;
                        case "EstoqueEstrangeiro":
                            using (var comando = new SqlCommand($@"INSERT INTO EstoqueEstrangeiro(SERIAL, LOCAL)
                                                                   SELECT SERIAL, LOCAL FROM Historico WHERE id = '{id}'
                                                                   DELETE FROM {destino} WHERE SERIAL = '{serial}'
                                                                   DELETE FROM HISTORICO WHERE SERIAL = '{serial}';", conexao)
                                                                )
                            {
                                await comando.ExecuteNonQueryAsync();
                            }
                            break;
                        case "DefeitoExterior":
                            using (var comando = new SqlCommand($@"INSERT INTO DefeitoExterior(SERIAL, LOCAL)
                                                                   SELECT SERIAL, LOCAL FROM Historico WHERE id = '{id}'
                                                                   DELETE FROM {destino} WHERE SERIAL = '{serial}'
                                                                   DELETE FROM HISTORICO WHERE SERIAL = '{serial}';", conexao)
                                                                )
                            {
                                await comando.ExecuteNonQueryAsync();
                            }
                            break;
                        case "DEVOLUCAO":
                            using (var comando = new SqlCommand($@"INSERT INTO DEVOLUCAO(SERIAL, CAIXA, DATA)
                                                                   SELECT SERIAL, CAIXA, DATA FROM Historico WHERE id = '{id}'
                                                                   DELETE FROM {destino} WHERE SERIAL = '{serial}'
                                                                   DELETE FROM HISTORICO WHERE SERIAL = '{serial}';", conexao)
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
                _logger.LogError(ex, $"Erro ao mover o serial {serial} para {origem}");
                return StatusCode(500);
            }
        }
    }
}
