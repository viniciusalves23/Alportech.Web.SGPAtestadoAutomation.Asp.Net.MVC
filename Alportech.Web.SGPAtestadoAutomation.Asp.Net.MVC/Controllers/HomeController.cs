using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using SGPAtestadoAutomation.Models;
using System.Diagnostics;
using OpenQA.Selenium.Support.UI;
using Alportech.Web.SGPAtestadoAutomation.Asp.Net.MVC.Models;

namespace SGPAtestadoAutomation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> EnviarAtestado(AtestadoModel model)
        {
            if (ModelState.IsValid)
            {
                var resultado = await RealizarAutomacao(model);
                if (resultado.Sucesso == true)
                {
                    TempData["Mensagem"] = resultado.Mensagem;
                    TempData["Titulo"] = "Atestado Gravado com Sucesso";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Mensagem"] = resultado.Mensagem;
                    TempData["Titulo"] = "Erro ao Gravar Atestado";
                    return RedirectToAction("Index");
                }
            }

            TempData["Mensagem"] = "O modelo Atestado esta inválido, alguma informação não foi passada corretamente.";
            TempData["Titulo"] = "Erro na Aplicação";
            return RedirectToAction("Index");
        }

        public async Task<ResultadoAutomacao> RealizarAutomacao(AtestadoModel model)
        {
            DateTime dataAtestado = model.DataAtestado;
            string dataFormatada = dataAtestado.ToString("dd/MM/yyyy");

            if ((dataAtestado.DayOfWeek == DayOfWeek.Saturday || dataAtestado.DayOfWeek == DayOfWeek.Sunday) && model.QuantidadeDias == 1)
            {
                return new ResultadoAutomacao { Sucesso = false, Mensagem = "O atestado não contém dias letivos para registro. Atestado com data de sábado ou domingo com duração de 1 dia. Nenhum registro necessário." };
            }

            List<DateTime> diasLetivos = new List<DateTime>();
            for (int i = 0; i < model.QuantidadeDias; i++)
            {
                DateTime diaAtual = dataAtestado.AddDays(i);

                if (diaAtual.DayOfWeek != DayOfWeek.Saturday && diaAtual.DayOfWeek != DayOfWeek.Sunday)
                {
                    diasLetivos.Add(diaAtual);
                }
            }

            if (diasLetivos.Count == 0)
            {
                return new ResultadoAutomacao { Sucesso = false, Mensagem = "O atestado não contém dias letivos para registro. Possivelmente a data do atestado é de sabado ou domingo e a quantidade de dias não contem nenhum dia letivo para registro." };
            }

            var options = new ChromeOptions();
            options.AddArgument("headless");
            options.AddArgument("disable-gpu");

            try
            {
                using (IWebDriver driver = new ChromeDriver(options))
                {
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                    driver.Navigate().GoToUrl("https://novosgp.sme.prefeitura.sp.gov.br/login");

                    Thread.Sleep(5000);

                    driver.FindElement(By.Id("usuario")).SendKeys("9242210");
                    driver.FindElement(By.Id("senha")).SendKeys("Black@062529");
                    driver.FindElement(By.XPath("//button[text()='Acessar']")).Click();

                    Thread.Sleep(5000);

                    driver.Navigate().GoToUrl("https://novosgp.sme.prefeitura.sp.gov.br/diario-classe/frequencia-plano-aula");

                    Thread.Sleep(5000);

                    foreach (var diaLetivo in diasLetivos)
                    {
                        var dataInput = driver.FindElement(By.Id("SGP_DATE_SELECIONAR_DATA_FREQUENCIA_PLANO_AULA"));

                        dataInput.SendKeys(Keys.Control + 'a');
                        dataInput.SendKeys(Keys.Delete);

                        Thread.Sleep(3000);

                        // Formatar a data corretamente
                        string dataFormatadaDiaLetivo = diaLetivo.ToString("dd/MM/yyyy");
                        dataInput.SendKeys(dataFormatadaDiaLetivo);
                        dataInput.SendKeys(Keys.Enter);

                        Thread.Sleep(3000);

                        driver.FindElement(By.Id("expandir-retrair-frequencia-collapse")).Click();

                        Thread.Sleep(3000);

                        string xpathLinhaAluno = $"//tr[td/div[contains(., '{model.NomeAluno}')]]";
                        IWebElement linhaAluno = driver.FindElement(By.XPath(xpathLinhaAluno));

                        if (linhaAluno != null)
                        {
                            IWebElement terceiroTd = linhaAluno.FindElement(By.CssSelector("td:nth-child(3)"));
                            IWebElement svgIcon = terceiroTd.FindElement(By.ClassName("fa-circle-xmark"));

                            if (svgIcon != null)
                            {
                                svgIcon.Click();
                            }
                        }

                        Thread.Sleep(3000);

                        var botaoAlterar = driver.FindElement(By.Id("SGP_BUTTON_ALTERAR_CADASTRAR"));
                        botaoAlterar.Click();

                        Thread.Sleep(3000);

                        IWebElement primeiroTd = linhaAluno!.FindElement(By.CssSelector("td:first-child"));

                        IWebElement svgIconModalMotivoAusencia = primeiroTd.FindElement(By.CssSelector("svg.fa-pen-to-square, svg.svg-inline--fa.fa-eye"));
                        svgIconModalMotivoAusencia.Click();

                        Thread.Sleep(3000);

                        IWebElement selectMotivoAusencia = driver.FindElement(By.Id("motivo-ausencia"));

                        bool campoPreenchido = driver.FindElements(By.XPath("//span[contains(@class, 'ant-select-selection-item') and contains(text(), 'Atestado Médico do Aluno')]")).Count > 0;

                        if (campoPreenchido)
                        {
                            IWebElement botaoLimpar = driver.FindElement(By.CssSelector(".ant-select-clear"));
                            botaoLimpar.Click();
                            Thread.Sleep(2000);
                        }

                        selectMotivoAusencia.Click();

                        Thread.Sleep(2000);

                        IWebElement opcaoAtestadoMedicoAluno = driver.FindElement(By.XPath("//div[@id='VALOR_1' and @title='Atestado Médico do Aluno']"));
                        opcaoAtestadoMedicoAluno.Click();

                        Thread.Sleep(2000);

                        if (model.AnexoAtestado != null)
                        {
                            driver.FindElement(By.ClassName("jodit-wysiwyg")).SendKeys(".");

                            using (var memoryStream = new MemoryStream())
                            {
                                await model.AnexoAtestado.CopyToAsync(memoryStream);
                                var imageBytes = memoryStream.ToArray();
                                var base64String = Convert.ToBase64String(imageBytes);
                                var imageUrl = $"data:{model.AnexoAtestado.ContentType};base64,{base64String}";
                                string script = $"var img = document.createElement('img'); img.src = '{imageUrl}'; document.querySelector('.jodit-wysiwyg').appendChild(img);";
                                ((IJavaScriptExecutor)driver).ExecuteScript(script);
                            }
                        }

                        Thread.Sleep(3000);

                        var botaoSalvar = driver.FindElement(By.Id("btn-salvar-anotacao"));
                        botaoSalvar.Click();

                        Thread.Sleep(2000);
                    }

                    Console.WriteLine($"Atestado anexado para o aluno {model.NomeAluno}. Data atestado: {model.DataAtestado.ToShortDateString()}. Qtde dias: {model.QuantidadeDias}.");
                    return new ResultadoAutomacao
                    {
                        Sucesso = true,
                        Mensagem = $"Atestado anexado para o aluno {model.NomeAluno}.\n\nData atestado: {model.DataAtestado.ToShortDateString()}.\n\nQuantidade dias: {model.QuantidadeDias}.\n\nAtestado registrado nas seguintes datas letivas:\n {string.Join(", ", diasLetivos.Select(d => d.ToString("dd/MM/yyyy")))}"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro na automação: " + ex.Message);
                return new ResultadoAutomacao { Sucesso = false, Mensagem = "Erro na automação: " + ex.Message };
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}