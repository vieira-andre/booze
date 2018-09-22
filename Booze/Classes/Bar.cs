using System.Device.Location;
using System.Threading;

namespace Booze
{
    public class Bar
    {
        public int idx { get; set; }
        public string nome { get; set; }
        public string bairro { get; set; }
        public string imagem { get; set; }
        public string endereco { get; set; }
        public string[] telefone { get; set; }
        public string horarios { get; set; }
        public GeoCoordinate coordenadas { get; set; }

        public Bar(int idx, string nome, string bairro, string endereco, string[] telefone, string horarios, GeoCoordinate coordenadas)
        {
            this.idx = idx;
            this.nome = nome;
            this.bairro = bairro;
            this.endereco = endereco;
            this.telefone = telefone;
            this.horarios = AdequaHorarios(horarios);
            this.coordenadas = coordenadas;
        }

        public Bar(int idx, string nome, string bairro, string imagem, string endereco, string[] telefone, string horarios, GeoCoordinate coordenadas)
        {
            this.idx = idx;
            this.nome = nome;
            this.bairro = bairro;
            this.imagem = imagem;
            this.endereco = endereco;
            this.telefone = telefone;
            this.horarios = AdequaHorarios(horarios);
            this.coordenadas = coordenadas;
        }

        private static string[] pt = new string[] { "segunda", "terça", "quarta", "quinta", "sexta", "sábado", "domingo", "todos os dias" };
        private static string[] en = new string[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday", "Everyday" };

        private static string AdequaHorarios(string entrada)
        {
            if (!Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.Equals("pt"))
            {
                for (int i = 0; i < pt.Length; i++)
                {
                    if (entrada.Contains(pt[i]))
                        entrada = entrada.Replace(pt[i], en[i]);
                }

                if (entrada.Contains(" a "))
                    entrada = entrada.Replace(" a ", " to ");

                if (entrada.Contains("às") || entrada.Contains("à")) {
                    entrada = entrada.Replace("às", "-");
                    entrada = entrada.Replace("à", "-");
                }

                if (entrada.Contains("h "))
                    entrada = entrada.Replace("h ", ":00 ");

                for (int y = 0; y <= 5; y++)
                {
                    if (entrada.Contains("h" + y.ToString()))
                        entrada = entrada.Replace("h" + y.ToString(), ":" + y.ToString());
                }

                for (int x = 0; x <= 9; x++)
                {
                    if (entrada.Contains(x.ToString() + "h"))
                        entrada = entrada.Replace(x.ToString() + "h", x.ToString() + ":00");
                }
            }

            return entrada;
        }
    }
}