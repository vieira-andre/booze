using Booze.Resources;

namespace Booze.Classes
{
    public class DiaSemana
    {
        public static IdxDia Segunda { get { return new IdxDia(0, AppResources.DiaSemana_Segunda); } }
        public static IdxDia Terca { get { return new IdxDia(1, AppResources.DiaSemana_Terca); } }
        public static IdxDia Quarta { get { return new IdxDia(2, AppResources.DiaSemana_Quarta); } }
        public static IdxDia Quinta { get { return new IdxDia(3, AppResources.DiaSemana_Quinta); } }
        public static IdxDia Sexta { get { return new IdxDia(4, AppResources.DiaSemana_Sexta); } }
        public static IdxDia Sabado { get { return new IdxDia(5, AppResources.DiaSemana_Sabado); } }
        public static IdxDia Domingo { get { return new IdxDia(6, AppResources.DiaSemana_Domingo); } }

        public struct IdxDia
        {
            public byte idx { get; set; }
            public string dia { get; set; }

            public IdxDia(byte idx, string dia) : this()
            {
                this.idx = idx;
                this.dia = dia;
            }
        }
    }
}
