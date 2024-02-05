using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//[CommandAlias("printer")]
public class Sample_Printer : Command {

    [CommandParameter("pos", true)]
    public float[] position { get => GetDynamicParameter<float[]>(default); set => SetDynamicParameter(value); }
    
    [CommandParameter("id", true)]
    public string id { get => GetDynamicParameter(string.Empty); set => SetDynamicParameter(value); }

    public override async Task ExecuteAsync() {
        if (Service.TryGetService<ScenarioService>(out var service) && service.TryGetPrinter(out var printer)) {
            // Visible Task
            var list = new List<Task> { printer.ChangeVisible(isVisible) };
            // Change Position Task
            if (string.IsNullOrEmpty(id) == false) {
                var target = service.GetAllActor()?.Where(x => x.Id.Equals(id)).FirstOrDefault();
                if (target != null) {
                    switch (target) {
                        default:
                            list.Add(GetChangePositionTask(printer));
                            break;
                    }
                }
            } else {
                list.Add(Task.WhenAll(GetChangePositionTask(printer)));
            }
            
            await Task.WhenAll(list);
        }
        
        await Task.Delay(TimeSpan.FromSeconds(delay));
    }

    private Task GetChangePositionTask(IScenarioActor printer) => Task.WhenAll(printer.ChangePosition(position.ToVector3()));
}
