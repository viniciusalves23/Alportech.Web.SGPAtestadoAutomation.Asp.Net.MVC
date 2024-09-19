using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using SGPAtestadoAutomation.Models;
using System.Diagnostics;
using OpenQA.Selenium.Support.UI;

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
        public IActionResult EnviarAtestado(AtestadoModel model)
        {
            if (ModelState.IsValid)
            {
                // Iniciar automa��o com Selenium
                bool sucesso = RealizarAutomacao(model);
                if (sucesso)
                {
                    return Json(new { sucesso = true });
                }
            }
            return Json(new { sucesso = false });
        }


        public bool RealizarAutomacao(AtestadoModel model)
        {
            var options = new ChromeOptions();
            //options.AddArgument("start-maximized"); // Abrir o navegador maximizado

            try
            {
                using (IWebDriver driver = new ChromeDriver(options))
                {
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                    // Acessar a p�gina de login do SGP
                    driver.Navigate().GoToUrl("https://novosgp.sme.prefeitura.sp.gov.br/login");

                    // Esperar 5 segundos
                    Thread.Sleep(5000);

                    // Preencher campos de login
                    driver.FindElement(By.Id("usuario")).SendKeys("9242210");
                    driver.FindElement(By.Id("senha")).SendKeys("Black@062529");
                    driver.FindElement(By.XPath("//button[text()='Acessar']")).Click();

                    // Esperar 5 segundos
                    Thread.Sleep(5000);

                    // Aguarde a p�gina de frequ�ncias carregar
                    driver.Navigate().GoToUrl("https://novosgp.sme.prefeitura.sp.gov.br/diario-classe/frequencia-plano-aula");

                    // Esperar 5 segundos
                    Thread.Sleep(5000);

                    // Preencher a data de frequ�ncia com a data do atestado
                    var dataInput = driver.FindElement(By.Id("SGP_DATE_SELECIONAR_DATA_FREQUENCIA_PLANO_AULA"));
                    dataInput.SendKeys(model.DataAtestado.ToString("dd/MM/yyyy"));
                    dataInput.SendKeys(Keys.Enter);

                    // Esperar 5 segundos
                    Thread.Sleep(5000);

                    // Clicar no bot�o para expandir o campo de frequ�ncia
                    IWebElement expandButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id("expandir-retrair-frequencia-collapse")));
                    expandButton.Click();

                    // Esperar 3 segundos
                    Thread.Sleep(3000);

                    // Para lidar com a quantidade de dias do atestado
                    DateTime dataAtestado = model.DataAtestado;

                    for (int i = 0; i < model.QuantidadeDias; i++)
                    {
                        string xpathLinhaAluno = $"//tr[td/div[contains(., '{model.NomeAluno}')]]";
                        IWebElement linhaAluno = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.XPath(xpathLinhaAluno)));

                        if (linhaAluno != null)
                        {
                            // Dentro do <tr> encontrado, localize o terceiro <td> (contem icones de presen�a e falta)
                            IWebElement terceiroTd = linhaAluno.FindElement(By.XPath("./td[3]"));

                            // Dentro do terceiro <td>, localize o �cone SVG com a classe 'fa-circle-xmark' (ICONE FALTA)
                            IWebElement svgIcon = terceiroTd.FindElement(By.CssSelector("svg.fa-circle-xmark"));

                            if (svgIcon != null)
                            {
                               //Marcar falta
                                svgIcon.Click();
                            }
                            else
                            {
                                Console.WriteLine("�cone de falta n�o encontrado.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Linha do aluno n�o encontrada.");
                        }

                        // Esperar 3 segundos
                        Thread.Sleep(3000);

                        // Clicar no bot�o Alterar ou Cadastrar
                        var botaoAlterar = driver.FindElement(By.Id("SGP_BUTTON_ALTERAR_CADASTRAR"));
                        botaoAlterar.Click();

                        // Esperar 10 segundos
                        Thread.Sleep(10000);

                        // Anexar o atestado se for o primeiro dia
                        if (i == 0 && model.AnexoAtestado != null)
                        {
                            var inputAnexo = driver.FindElement(By.Id("anexo"));
                            // Salvar o arquivo temporariamente
                            string caminhoTemp = Path.GetTempFileName();
                            using (var stream = new FileStream(caminhoTemp, FileMode.Create))
                            {
                                model.AnexoAtestado.CopyTo(stream);
                            }
                            inputAnexo.SendKeys(caminhoTemp); // O caminho tempor�rio para o arquivo do atestado
                        }

                        // Salvar
                        var botaoSalvar = driver.FindElement(By.Id("btn-salvar-anotacao"));
                        botaoSalvar.Click();

                        // Avan�ar para o pr�ximo dia
                        if (i < model.QuantidadeDias - 1)
                        {
                            dataAtestado = dataAtestado.AddDays(1);
                            dataInput.Clear();
                            dataInput.SendKeys(dataAtestado.ToString("dd/MM/yyyy"));
                            dataInput.SendKeys(Keys.Enter);
                        }
                    }

                    // Mensagem de sucesso
                    Console.WriteLine($"Atestado anexado para o aluno {model.NomeAluno}. Data atestado: {model.DataAtestado.ToShortDateString()}. Qtde dias {model.QuantidadeDias}.");
                    return true; // Automa��o realizada com sucesso
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro na automa��o: " + ex.Message);
                return false;
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
