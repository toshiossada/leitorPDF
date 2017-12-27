using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LeitorEcac.Entities
{
    public class ECAC_DOCUMENTO
    {
        public string CNPJ { get; set; }
        public string Nome { get; set; }
        public DateTime PeriodoApuracao { get; set; }
        public DateTime DataVencimento { get; set; }
        public string NumeroDocumento { get; set; }
        public List<ECAC_COMPOSICAO> Composicao { get; set; }


        /// <summary>
        /// Realiza a leitura do documento e-CAC e converte para string, posteriormente chama os metodos de extração dos dados do e-CAC
        /// </summary>
        /// <param name="FileName">Path do arquivo de PDF</param>
        /// <returns>Retorna lista de e-Cac's encontrados</returns>
        /// <remarks>Toshi Ossada - toshiossada@gmail.com</remarks>
        public List<ECAC_DOCUMENTO> LerConteudoPDFECAC(string FileName)
        {
            PdfReader pdfreader = new PdfReader(FileName);
            List<ECAC_DOCUMENTO> ecac = new List<ECAC_DOCUMENTO>();

            for (int i = 1; i <= pdfreader.NumberOfPages; i++)
            {
                ITextExtractionStrategy iTextextStrat = new SimpleTextExtractionStrategy();

                PdfReader reader = new PdfReader(FileName);
                String extractText = PdfTextExtractor.GetTextFromPage(reader, i, iTextextStrat);

                extractText = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(extractText)));

                

                var dt = extrairData(extractText);

                if (DateTime.Compare(dt, Convert.ToDateTime("01/11/2017")) < 0) {
                    ecac.AddRange(extrairEcacV1(extractText));
                }else
                {
                    ecac.AddRange(extrairEcacV2(extractText));
                }
                


                reader.Close();

            }

            pdfreader.Close();

            return ecac;
        }

        /// <summary>
        /// Exatrai a data de download do e-Cac
        /// </summary>
        /// <param name="texto">Texto extraido do PDF da e-CAC</param>
        /// <returns>Retorna data de emissão do e-CAC</returns>
        /// <remarks>Toshi Ossada - toshiossada@gmail.com</remarks>
        private DateTime extrairData(String texto)
        {
            DateTime dtExpedicao = new DateTime();
            Regex ER = new Regex(@"Comprovante emitido às (?<hora>.*) de (?<data>[\w:/\.]+) \(horário de Brasília\)", RegexOptions.None);
            if (ER.IsMatch(texto))
            {
                //Pegar a data de emissão do documento
                Match matchData = ER.Match(texto);
                dtExpedicao = Convert.ToDateTime(matchData.Result("${data}"));
            }

            return dtExpedicao;
        }

        #region Versao 2
        /// <summary>
        /// Extrai informações do e-CAC levando em consideração que se trata da segunda versão
        /// </summary>
        /// <param name="texto">Texto extraido do PDF da e-CAC</param>
        /// <returns>Retorna Lista de documentos da  e-CAC extraido do texto</returns>
        /// <remarks>Toshi Ossada - toshiossada@gmail.com</remarks>
        private List<ECAC_DOCUMENTO> extrairEcacV2(String texto)
        {
            List<ECAC_DOCUMENTO> lstEcac = new List<ECAC_DOCUMENTO>();

            //Faz a quebra por doumento do eCac
            string[] lsteCac = Regex.Split(texto.Trim(), @"Data de Vencimento([\s\S]*?)\nData de Vencimento");
            foreach (string txteCac in lsteCac.Where(r => !String.IsNullOrEmpty(r.Trim())).Select(r => r.Replace("Data de Vencimento\n", "").Trim()))
            {
                ECAC_DOCUMENTO ecac = new ECAC_DOCUMENTO();

                Regex ERCabecalho = new Regex(@"(?<cabecalho>[\s\S]*?)Comprovamos que consta, nos sistemas de controle da Receita Federal do Brasil", RegexOptions.None);
                if (ERCabecalho.IsMatch(texto))
                {
                    //Pega cabeçalho do eCac
                    Match matchData = ERCabecalho.Match(txteCac);

                    ecac = extrairCabecalho(matchData);
                }

                Regex ERComposicao = new Regex(@"Composição do Documento de Arrecadação(?<composicao>[\s\S]*?)Totais", RegexOptions.None);
                if (ERComposicao.IsMatch(texto))
                {
                    //Pega cada composição do documento
                    Match matchData = ERComposicao.Match(txteCac);
                    ecac.Composicao = extrairComposicaoECAC(matchData, ecac);
                    lstEcac.Add(ecac);
                }
            }

            return lstEcac;
        }

        /// <summary>
        /// Extrair valores do cabecalho da e-CAC
        /// </summary>
        /// <param name="matchData">Match encontrado dos valores do regex referente ao cabeçalho</param>
        /// <returns>Retorna objeto e-Cac ja com os valores do cabeçalho</returns>
        /// <remarks>Toshi Ossada - toshiossada@gmail.com</remarks>
        private ECAC_DOCUMENTO extrairCabecalho(Match matchData)
        {
            ECAC_DOCUMENTO ecac = new ECAC_DOCUMENTO();

            var cabecalhos = matchData.Result("${cabecalho}").Trim();
            var cabecalho = cabecalhos.Split(new string[] { "\n" }, StringSplitOptions.None);
            var datas = cabecalho[1].Split(' ');


            ecac.CNPJ = cabecalho[0].Split(' ')[0].Trim();
            ecac.Nome = cabecalho[0].Trim().Substring(ecac.CNPJ.Length).Trim();
            ecac.PeriodoApuracao = Convert.ToDateTime(datas[0].Trim());
            ecac.DataVencimento = Convert.ToDateTime(datas[1].Trim());
            ecac.NumeroDocumento = datas[2].Trim();

            return ecac;
        }

        /// <summary>
        /// Extrair valores de composição da e-CAC
        /// </summary>
        /// <param name="matchData">Match encontrado dos valores do regex referente a composição de valores</param>
        /// <param name="ecac">Objeto e-CAC a ser trabalhado</param>
        /// <returns>Retorna lista de objetos de composições de valores</returns>
        /// <remarks>Toshi Ossada - toshiossada@gmail.com</remarks>
        private List<ECAC_COMPOSICAO> extrairComposicaoECAC(Match matchData, ECAC_DOCUMENTO ecac)
        {
            List<String> lstComposicao = matchData.Result("${composicao}").Split(new string[] { "\n" }, StringSplitOptions.None).Select(r => r.Trim()).Where(r => !String.IsNullOrEmpty(r)).ToList();

            List<ECAC_COMPOSICAO> l = new List<ECAC_COMPOSICAO>();
            foreach (var composicao in lstComposicao)
            {
                l.Add(extrairComposicaoECAC(composicao));
            }

            return l;
        }

        /// <summary>
        /// Extrair valores da linha de composição de valores
        /// </summary>
        /// <param name="composicao">Linha da composição</param>
        /// <returns>Retorna objeto de composião de valores</returns>
        /// <remarks>Toshi Ossada - toshiossada@gmail.com</remarks>
        private ECAC_COMPOSICAO extrairComposicaoECAC(String composicao)
        {
            var valores = composicao.Replace("-", "0").Split(' ').Reverse().ToList<string>();

            return new ECAC_COMPOSICAO()
            {
                Codigo = composicao.Split(' ')[0],
                Descricao = composicao.Substring(composicao.Split(' ')[0].Length, (composicao.Length - composicao.Split(' ')[0].Length) - (valores[3].Length + 1 + valores[2].Length + 1 + valores[1].Length + 1 + valores[0].Length + 1)).Trim(),
                ValorPrincipal = Convert.ToDecimal(valores[3]),
                ValorMulta = Convert.ToDecimal(valores[2]),
                ValorJuros = Convert.ToDecimal(valores[1]),
                ValorTotal = Convert.ToDecimal(valores[0])
            };
        }
        #endregion

        #region Versao 1
        /// <summary>
        /// Extrai informações do e-CAC levando em consideração que se trata da primeira versão
        /// </summary>
        /// <param name="texto">Texto extraido do PDF da e-CAC</param>
        /// <returns>Retorna Lista de documentos da  e-CAC extraido do texto</returns>
        /// <remarks>Toshi Ossada - toshiossada@gmail.com</remarks>
        private List<ECAC_DOCUMENTO> extrairEcacV1(String texto)
        {
            List<ECAC_DOCUMENTO> lstEcac = new List<ECAC_DOCUMENTO>();

            //Faz a quebra por doumento do eCac
            string[] lsteCac = Regex.Split(texto.Trim(), @"Ministério da Fazenda([\s\S]*?)\nMinistério da Fazenda");
            foreach (string txteCac in lsteCac.Where(r => !String.IsNullOrEmpty(r.Trim())).Select(r => r.Replace("Data de Vencimento\n", "").Trim()))
            {

                Regex ERCabecalho = new Regex(@"características abaixo:\n(?<cabecalho>[\s\S]*?)Comprovante emitido às", RegexOptions.None);
                if (ERCabecalho.IsMatch(texto))
                {
                    //Pega cabeçalho do eCac
                    Match matchData = ERCabecalho.Match(txteCac);

                    var linhas = matchData.Result("${cabecalho}").Split('\n').Where(r => !String.IsNullOrEmpty(r.Trim())).ToList();

                    ECAC_DOCUMENTO oeCac = new ECAC_DOCUMENTO()
                    {
                        CNPJ = linhas.Where(r => r.StartsWith("Número de inscrição no CNPJ")).FirstOrDefault().Replace("Número de inscrição no CNPJ", "").Replace(":", "").Trim(),
                        PeriodoApuracao = Convert.ToDateTime(linhas.Where(r => r.StartsWith("Período de Apuração")).FirstOrDefault().Replace("Período de Apuração", "").Replace(":", "").Trim()),
                        DataVencimento = Convert.ToDateTime(linhas.Where(r => r.StartsWith("Data de Vencimento")).FirstOrDefault().Replace("Data de Vencimento", "").Replace(":", "").Trim()),
                        Nome = linhas.Where(r => r.StartsWith("Contribuinte")).FirstOrDefault().Replace("Contribuinte", "").Replace(":", "").Trim(),
                        NumeroDocumento = linhas.Where(r => r.StartsWith("Número do Documento")).FirstOrDefault().Replace("Número do Documento", "").Replace(":", "").Trim(),
                        Composicao = new List<ECAC_COMPOSICAO>()
                    };

                    var lstComposicao = linhas.Where(r => r.StartsWith("Valor no Código de Receita ")).ToList();


                    foreach (var item in lstComposicao)
                    {

                        Regex ERComposicao = new Regex(@"Valor no Código de Receita (?<codigo>.*):(?<valor>.*)", RegexOptions.None);
                        if (ERComposicao.IsMatch(item))
                        {
                            //Pegar a data de emissão do documento
                            Match matchDataC = ERComposicao.Match(item);
                            oeCac.Composicao.Add(new ECAC_COMPOSICAO()
                            {
                                Codigo = matchDataC.Result("${codigo}").Trim(),
                                ValorPrincipal = Convert.ToDecimal(matchDataC.Result("${valor}").Trim())
                            });

                        }
                    }

                    lstEcac.Add(oeCac);
                }
            }

            return lstEcac;
        }
        #endregion
    }
}