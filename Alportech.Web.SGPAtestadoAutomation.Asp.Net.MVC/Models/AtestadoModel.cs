using System.ComponentModel.DataAnnotations;

namespace SGPAtestadoAutomation.Models
{
    public class AtestadoModel
    {
        public string? NomeAluno { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime DataAtestado { get; set; }
        public int QuantidadeDias { get; set; }
        public IFormFile? AnexoAtestado { get; set; }
    }
}