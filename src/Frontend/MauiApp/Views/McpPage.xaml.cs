namespace MauiApp.Views;

public partial class McpPage : ContentPage
{
	public McpPage()
	{
		InitializeComponent();
	}

    private async void BtnSend_Clicked(object sender, EventArgs e)
    {
        var query = EntQuery.Text;
        if (string.IsNullOrWhiteSpace(query)) return;

        EdHistory.Text += $"Tú: {query}\n";
        EntQuery.Text = "";

        var response = await LoginPage.ApiClient.QueryMcpAsync(query);
        EdHistory.Text += $"MCP: {response}\n\n";
    }
}
