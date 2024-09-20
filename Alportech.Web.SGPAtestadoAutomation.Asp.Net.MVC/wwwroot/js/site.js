document.addEventListener('DOMContentLoaded', function () {
    // Inicializar o calendário Flatpickr
    flatpickr("#data", {
        dateFormat: "d/m/Y",
        defaultDate: "today",
        locale: {
            firstDayOfWeek: 1,
            weekdays: {
                shorthand: ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'],
                longhand: ['Domingo', 'Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado'],
            },
            months: {
                shorthand: ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'],
                longhand: ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'],
            },
        }
    });

    // Manipular o envio do formulário
    document.getElementById('atestadoForm').addEventListener('submit', function (e) {
        e.preventDefault();

        // Verificar se todos os campos estão preenchidos
        const campos = [
            { id: 'aluno', mensagem: 'Por favor, selecione um aluno.' },
            { id: 'data', mensagem: 'Por favor, selecione uma data.' },
            { id: 'dias', mensagem: 'Por favor, informe a quantidade de dias.' },
            { id: 'anexo', mensagem: 'Por favor, anexe o atestado.' }
        ];

        for (let campo of campos) {
            const elemento = document.getElementById(campo.id);
            if (!elemento.value) {
                alert(campo.mensagem);
                elemento.focus();
                return;
            }
        }

        // Capturar o nome do aluno selecionado
        const alunoSelect = document.getElementById('aluno');
        const nomeAluno = alunoSelect.options[alunoSelect.selectedIndex].text;

        // Exibir a modal de progresso com o nome do aluno
        document.getElementById('mensagemModal').innerText = `Adicionando atestado para o aluno ${nomeAluno}`;
        $('#progressoModal').modal('show');

        // Capturar dados do formulário
        const formData = new FormData();
        formData.append('NomeAluno', nomeAluno);
        formData.append('DataAtestado', document.getElementById('data').value);
        formData.append('QuantidadeDias', document.getElementById('dias').value);
        formData.append('AnexoAtestado', document.getElementById('anexo').files[0]);

        // Enviar dados para o backend via AJAX
        fetch('/Home/EnviarAtestado', {
            method: 'POST',
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                document.getElementById('progressoModal').classList.remove('show');
                document.querySelector('.modal-backdrop').remove();

                if (data.sucesso) {
                    alert(`Sucesso: ${data.mensagem}`);
                } else {
                    alert(`Erro: ${data.mensagem}`);
                }
            })
            .catch(error => {
                document.getElementById('progressoModal').classList.remove('show');
                document.querySelector('.modal-backdrop').remove();
                alert(`Erro: ${error}`);
            });
    });
});