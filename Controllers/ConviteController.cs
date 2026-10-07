using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System;

namespace SiteCasamento_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConviteController : ControllerBase
    {
        // ATENÇÃO: Cole aqui novamente a sua string de conexão que estava funcionando!
        private readonly string _connectionString = "Server=db66733.databaseasp.net; Database=db66733; User Id=db66733; Password=5o-ZgD8_9=Aw; Encrypt=False; MultipleActiveResultSets=True;";
       
        [HttpGet("{codigo}")]
        public IActionResult ObterConvite(string codigo)
        {
            codigo = codigo.ToUpper();
            var convidados = new List<ConvidadoDto>();
            string familiaNome = string.Empty;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Lê apenas os convidados estritamente vinculados ao código informado
                var command = new SqlCommand("SELECT Nome, Familia, Confirmado FROM lista WHERE Codigo = @Codigo", connection);
                command.Parameters.AddWithValue("@Codigo", codigo);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        familiaNome = reader["Familia"].ToString() ?? "";

                        // Leitura segura: pega o valor como texto primeiro para evitar erro de string vazia ou nula
                        string valorConfirmado = reader["Confirmado"].ToString() ?? "";

                        // Se o valor for "1" ou "True", fica true. Se for vazio, nulo ou "0", fica false.
                        bool statusConfirmado = valorConfirmado == "1" || valorConfirmado.Equals("True", StringComparison.OrdinalIgnoreCase);

                        convidados.Add(new ConvidadoDto
                        {
                            Name = reader["Nome"].ToString(),
                            Confirmed = statusConfirmado
                        });
                    }
                }
            }

            // Se não encontrou ninguém com esse código, retorna o erro
            if (convidados.Count == 0)
                return NotFound(new { error = "Código não encontrado. Verifique e tente novamente." });

            // Retorna a lista exata, permitindo a exibição na tela sem travas
            return Ok(new
            {
                code = codigo,
                familyName = "Família " + familiaNome,
                guests = convidados
            });
        }

        [HttpPost("salvar")]
        public IActionResult SalvarConfirmacao([FromBody] ConfirmacaoRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (request.Guests != null)
                {
                    foreach (var guest in request.Guests)
                    {
                        // Atualiza o campo Confirmado (0 ou 1) estritamente para o nome vinculado àquele código
                        var command = new SqlCommand("UPDATE lista SET Confirmado = @Status WHERE Codigo = @Codigo AND Nome = @Nome", connection);
                        command.Parameters.AddWithValue("@Status", guest.Confirmed ? 1 : 0);
                        command.Parameters.AddWithValue("@Codigo", request.Code);
                        command.Parameters.AddWithValue("@Nome", guest.Name);
                        command.ExecuteNonQuery();
                    }
                }
            }

            return Ok();
        }
        [HttpGet("admin")]
        public IActionResult ObterTodos()
        {
            var familiasMap = new Dictionary<string, FamiliaAdminDto>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // Busca todo mundo da tabela
                var command = new SqlCommand("SELECT Codigo, Familia, Nome, Confirmado FROM lista ORDER BY Familia", connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string cod = reader["Codigo"].ToString() ?? "";
                        if (!familiasMap.ContainsKey(cod))
                        {
                            familiasMap[cod] = new FamiliaAdminDto
                            {
                                Code = cod,
                                FamilyName = "Família " + reader["Familia"].ToString(),
                                Guests = new List<ConvidadoDto>()
                            };
                        }

                        string valorConfirmado = reader["Confirmado"].ToString() ?? "";
                        bool statusConfirmado = valorConfirmado == "1" || valorConfirmado.Equals("True", StringComparison.OrdinalIgnoreCase);

                        familiasMap[cod].Guests.Add(new ConvidadoDto
                        {
                            Name = reader["Nome"].ToString(),
                            Confirmed = statusConfirmado
                        });
                    }
                }
            }
            // Retorna a lista de todas as famílias agrupadas
            return Ok(familiasMap.Values);
        }
    }

    // Classes auxiliares (DTOs) com a interrogação (?) para evitar avisos de valores nulos
    public class ConvidadoDto
    {
        public string? Name { get; set; }
        public bool Confirmed { get; set; }
    }

    public class ConfirmacaoRequest
    {
        public string? Code { get; set; }
        public List<ConvidadoDto>? Guests { get; set; }
    }
    public class FamiliaAdminDto
    {
        public string? Code { get; set; }
        public string? FamilyName { get; set; }
        public List<ConvidadoDto>? Guests { get; set; }
    }
}