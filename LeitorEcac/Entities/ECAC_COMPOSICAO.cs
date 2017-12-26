namespace LeitorEcac.Entities
{
    public class ECAC_COMPOSICAO
    {
        public string Codigo { get; set; }
        public string Descricao { get; set; }
        public decimal ValorPrincipal { get; set; }
        public decimal ValorMulta { get; set; }
        public decimal ValorJuros { get; set; }
        public decimal ValorTotal { get; set; }
    }
}