using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Groups.Components;

public partial class PayoutAccountEditor
{
    [Parameter]
    public GroupPayoutAccount? ExistingAccount { get; set; }

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public bool IsError { get; set; }

    [Parameter]
    public EventCallback<GroupPayoutAccount> OnSave { get; set; }

    private bool IsEditing = false;
    private GroupPayoutAccount FormData = new();

    protected override void OnParametersSet()
    {
        if (ExistingAccount != null && !IsEditing)
        {
            FormData = new GroupPayoutAccount
            {
                PixKeyType = ExistingAccount.PixKeyType,
                PixKeyValue = ExistingAccount.PixKeyValue,
                BeneficiaryName = ExistingAccount.BeneficiaryName,
                BeneficiaryCpf = ExistingAccount.BeneficiaryCpf,
                BankAccountNumber = ExistingAccount.BankAccountNumber
            };
        }
    }

    private void HandleEdit()
    {
        IsEditing = true;
        FormData = new GroupPayoutAccount
        {
            PixKeyType = ExistingAccount!.PixKeyType,
            PixKeyValue = ExistingAccount.PixKeyValue,
            BeneficiaryName = ExistingAccount.BeneficiaryName,
            BeneficiaryCpf = ExistingAccount.BeneficiaryCpf,
            BankAccountNumber = ExistingAccount.BankAccountNumber
        };
    }

    private void HandleCancel()
    {
        IsEditing = false;
        FormData = new();
    }

    private void FillTestData()
    {
        FormData = new GroupPayoutAccount
        {
            PixKeyType = PixKeyType.Cpf,
            PixKeyValue = "12345678901",
            BeneficiaryName = "Organizador Teste",
            BeneficiaryCpf = "12345678901",
            BankAccountNumber = "123456"
        };
    }

    private async Task HandleSave()
    {
        if (string.IsNullOrWhiteSpace(FormData.PixKeyValue))
        {
            Message = "A chave PIX é obrigatória.";
            IsError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(FormData.BeneficiaryName))
        {
            Message = "O nome do beneficiário é obrigatório.";
            IsError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(FormData.BeneficiaryCpf))
        {
            Message = "O CPF do beneficiário é obrigatório.";
            IsError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(FormData.BankAccountNumber))
        {
            Message = "O número da conta é obrigatório.";
            IsError = true;
            return;
        }

        await OnSave.InvokeAsync(FormData);
    }

    private string GetPixKeyTypeLabel(PixKeyType type) => type switch
    {
        PixKeyType.Cpf => "CPF",
        PixKeyType.Cnpj => "CNPJ",
        PixKeyType.Email => "E-mail",
        PixKeyType.Phone => "Telefone",
        PixKeyType.Random => "Chave Aleatória",
        _ => "Desconhecido"
    };
}
