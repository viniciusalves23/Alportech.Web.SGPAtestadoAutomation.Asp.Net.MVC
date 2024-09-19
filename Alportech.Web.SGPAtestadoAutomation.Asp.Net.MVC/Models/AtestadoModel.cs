namespace SGPAtestadoAutomation.Models
{
    public class AtestadoModel
    {
        public string? NomeAluno { get; set; }
        public DateTime DataAtestado { get; set; }
        public int QuantidadeDias { get; set; }
        public IFormFile? AnexoAtestado { get; set; }
    }
}