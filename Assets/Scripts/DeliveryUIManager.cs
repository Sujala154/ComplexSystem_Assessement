using UnityEngine;
using TMPro;

public class AgentUIController : MonoBehaviour
{
    public WorkshopAmbulance1 agent1;
    public WorkshopAmbulance1 agent2;
    public WorkshopAmbulance1 agent3;

    public TextMeshProUGUI agent1Text;
    public TextMeshProUGUI agent2Text;
    public TextMeshProUGUI agent3Text;

  void Update()
    {
        agent1Text.text =
            "Agent 1\n" +
            "Speed: " + agent1.currentSpeed.ToString("F1") + "\n" +
            "Distance: " + agent1.totalDistance.ToString("F1") + "\n" +
            "Patients: " + agent1.itemsCarried + "\n" +
            "Status: " + agent1.deliveryStatus;

        agent2Text.text =
            "Agent 2\n" +
            "Speed: " + agent2.currentSpeed.ToString("F1") + "\n" +
            "Distance: " + agent2.totalDistance.ToString("F1") + "\n" +
            "Patients: " + agent2.itemsCarried + "\n" +
            "Status: " + agent2.deliveryStatus;

        agent3Text.text =
            "Agent 3\n" +
            "Speed: " + agent3.currentSpeed.ToString("F1") + "\n" +
            "Distance: " + agent3.totalDistance.ToString("F1") + "\n" +
            "Patients: " + agent3.itemsCarried + "\n" +
            "Status: " + agent3.deliveryStatus;
    }

}
