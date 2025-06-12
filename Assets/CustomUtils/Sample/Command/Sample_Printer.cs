using System;
using System.Threading.Tasks;

//[CommandAlias("printer")]
public class Sample_Printer : Command {

    [CommandParameter("pos", true)]
    public float[] position { get => GetDynamicParameter<float[]>(default); set => SetDynamicParameter(value); }
    
    [CommandParameter("id", true)]
    public string id { get => GetDynamicParameter(string.Empty); set => SetDynamicParameter(value); }

    public override async Task ExecuteAsync() {
        if (Service.TryGetService<ScenarioService>(out var service)) {
            if (string.IsNullOrEmpty(id) == false) {
                if (service.GetAllActor().TryFirst(out var targetPrinter, x => x.Id == id)) {
                    await Task.WhenAll(targetPrinter.ChangeVisible(isVisible), GetChangePositionTask(targetPrinter));
                }
            } else {
                if (service.TryGetPrinter(out var defaultPrinter)) {
                    await Task.WhenAll(defaultPrinter.ChangeVisible(isVisible), GetChangePositionTask(defaultPrinter));    
                }
            }
        }
        
        await Task.Delay(TimeSpan.FromSeconds(delay));
    }

    private Task GetChangePositionTask(IScenarioActor actor) => actor.ChangePosition(position.ToVector3());
}
