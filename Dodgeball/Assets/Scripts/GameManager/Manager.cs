using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    [SerializeField] private bool save;
    [SerializeField] private bool trainSavedAgents;
    
    [SerializeField] private Text genText;
    
    private bool trainingStart;

    public GameObject agentPrefab;
    public GameObject target;

    private bool isTraining = false;
    public int populationSize;
    private int generationNumber = 0;
    private int[] layers = new int[] { 11, 30, 30, 30, 2 };
    private List<NeuralNetwork> nets;
    private List<Agent> agentList = null;

    [SerializeField] private List<GameObject> userInterface;

    [SerializeField] private bool trainWalk;
    [SerializeField] private bool trainAvoidWall;
    [SerializeField] private bool trainDodge;

    void Timer()
    {
        isTraining = false;
    }

    void Update()
    {
        if (isTraining == false && trainingStart)
        {
            if (generationNumber == 0)
            {
                InitAgentNeuralNetworks();
            }
            else
            {
                nets.Sort();

                if (save)
                {
                    Save();
                }

                for (int i = 0; i < populationSize / 2; i++)
                {
                    nets[i] = new NeuralNetwork(nets[i + (populationSize / 2)]);
                    nets[i].Mutate();

                    nets[i + (populationSize / 2)] = new NeuralNetwork(nets[i + (populationSize / 2)]);
                }

                for (int i = 0; i < populationSize; i++)
                {
                    nets[i].SetFitness(0f);
                }
            }

            generationNumber++;

            isTraining = true;
            Invoke("Timer", 15f);

            if (trainSavedAgents)
            {
                CreateAgentsFromSavedNetwork();
                trainSavedAgents = false;
            }
            else
            {
                CreateAgentBodies();
            }
        }

        genText.text = "Generation: " + generationNumber.ToString();
    }

    public void StartTraining()
    {
        trainingStart = true;

        foreach(GameObject elemnet in userInterface)
        {
            elemnet.SetActive(false);
        }
    }

    private void CreateAgentsFromSavedNetwork()
    {
        string savePath = "";

        if (trainWalk)
        {
            savePath = "agentWalk.json";

            if (!File.Exists(Path.Combine(Application.persistentDataPath, savePath)))
            {
                CreateAgentBodies();
                return;
            }
        }
        else if (trainAvoidWall)
        {
            savePath = "agentAvoidWall.json";

            if (!File.Exists(Path.Combine(Application.persistentDataPath, savePath)))
            {
                savePath = "agentWalk.json";

                if (!File.Exists(Path.Combine(Application.persistentDataPath, savePath)))
                {
                    CreateAgentBodies();
                    return;
                }
            }
        }
        else if (trainDodge)
        {
            savePath = "agentDodge.json";

            if (!File.Exists(Path.Combine(Application.persistentDataPath, savePath)))
            {
                savePath = "agentAvoidWall.json";

                if (!File.Exists(Path.Combine(Application.persistentDataPath, savePath)))
                {
                    savePath = "agentWalk.json";

                    if (!File.Exists(Path.Combine(Application.persistentDataPath, savePath)))
                    {
                        CreateAgentBodies();
                        return;
                    }
                }
            }
        }

        string filePath = Path.Combine(Application.persistentDataPath, savePath);

        if (!File.Exists(filePath))
        {
            return;
        }

        string json = File.ReadAllText(filePath);
        SavedNetwork savedNetwork = JsonUtility.FromJson<SavedNetwork>(json);

        int[] layers = { 11, 30, 30, 30, 2 };
        NeuralNetwork reconstructedNetwork = new NeuralNetwork(layers);

        float[][][] weights = new float[savedNetwork.layerArrays.Length][][];

        for (int i = 0; i < savedNetwork.layerArrays.Length; i++)
        {
            weights[i] = new float[savedNetwork.layerArrays[i].weightsArrays.Length][];
            for (int j = 0; j < savedNetwork.layerArrays[i].weightsArrays.Length; j++)
            {
                weights[i][j] = savedNetwork.layerArrays[i].weightsArrays[j].weights;
            }
        }

        reconstructedNetwork.SetWeights(weights);

        if (agentList != null)
        {
            for (int i = 0; i < agentList.Count; i++)
            {
                if (agentList[i] != null)
                    Destroy(agentList[i].gameObject);
            }
        }

        agentList = new List<Agent>();

        for (int i = 0; i < populationSize; i++)
        {
            Agent agent = ((GameObject)Instantiate(agentPrefab, new Vector3(UnityEngine.Random.Range(-5f, 5f), 2, UnityEngine.Random.Range(-5f, 5f)), agentPrefab.transform.rotation)).GetComponent<Agent>();
            agent.Init(reconstructedNetwork, target.transform);
            agentList.Add(agent);
            nets[i] = reconstructedNetwork;
        }
    }

    private void CreateAgentBodies()
    {
        if (agentList != null)
        {
            for (int i = 0; i < agentList.Count; i++)
            {
                if (agentList[i] != null)
                    Destroy(agentList[i].gameObject);
            }
        }

        agentList = new List<Agent>();

        for (int i = 0; i < populationSize; i++)
        {
            Agent agent = ((GameObject)Instantiate(agentPrefab, new Vector3(UnityEngine.Random.Range(-5f, 5f), 2, UnityEngine.Random.Range(-5f, 5f)), agentPrefab.transform.rotation)).GetComponent<Agent>();

            agent.Init(nets[i], target.transform);
            agentList.Add(agent);
        }
    }

    void InitAgentNeuralNetworks()
    {
        if (populationSize % 2 != 0)
        {
            populationSize = 20;
        }

        nets = new List<NeuralNetwork>();

        for (int i = 0; i < populationSize; i++)
        {
            NeuralNetwork net = new NeuralNetwork(layers);
            net.Mutate();
            nets.Add(net);
        }
    }

    private void Save()
    {
        NeuralNetwork bestNetwork = null;
        float bestFitness = float.MinValue;

        foreach (var net in nets)
        {
            if (net.GetFitness() > bestFitness)
            {
                bestFitness = net.GetFitness();
                bestNetwork = net;
            }
        }

        if (bestNetwork != null)
        {
            SavedNetwork savedNetwork = new SavedNetwork();
            savedNetwork.fitness = bestFitness;

            List<LayerWeightsArray> layerArrays = new List<LayerWeightsArray>();

            for (int i = 0; i < bestNetwork.GetWeights().Length; i++)
            {
                LayerWeightsArray layerArray = new LayerWeightsArray();
                List<WeightsArray> weightsArrays = new List<WeightsArray>();

                for (int j = 0; j < bestNetwork.GetWeights()[i].Length; j++)
                {
                    WeightsArray weightsArray = new WeightsArray();
                    weightsArray.weights = bestNetwork.GetWeights()[i][j];

                    weightsArrays.Add(weightsArray);
                }

                layerArray.weightsArrays = weightsArrays.ToArray();
                layerArrays.Add(layerArray);
            }

            savedNetwork.layerArrays = layerArrays.ToArray();

            string json = JsonUtility.ToJson(savedNetwork, true);

            string savePath = "";

            if (trainWalk)
            {
                savePath = "agentWalk.json";
            }
            else if (trainAvoidWall)
            {
                savePath = "agentAvoidWall.json";
            }
            else if (trainDodge)
            {
                savePath = "agentDodge.json";
            }

            string filePath = Path.Combine(Application.persistentDataPath, savePath);
            File.WriteAllText(filePath, json);
        }
    }

    public void LoadAgent()
    {
        foreach (GameObject elemnet in userInterface)
        {
            elemnet.SetActive(false);
        }

        string filePath = Path.Combine(Application.persistentDataPath, "bestNeuralNetwork.json");

        if (!File.Exists(filePath))
        {
            return;
        }

        string json = File.ReadAllText(filePath);
        SavedNetwork savedNetwork = JsonUtility.FromJson<SavedNetwork>(json);

        int[] layers = { 11, 30, 30, 30, 2 };
        NeuralNetwork reconstructedNetwork = new NeuralNetwork(layers);

        float[][][] weights = new float[savedNetwork.layerArrays.Length][][];

        for (int i = 0; i < savedNetwork.layerArrays.Length; i++)
        {
            weights[i] = new float[savedNetwork.layerArrays[i].weightsArrays.Length][];
            for (int j = 0; j < savedNetwork.layerArrays[i].weightsArrays.Length; j++)
            {
                weights[i][j] = savedNetwork.layerArrays[i].weightsArrays[j].weights;
            }
        }

        reconstructedNetwork.SetWeights(weights);

        Agent agent = Instantiate(agentPrefab, Vector3.zero, Quaternion.identity).GetComponent<Agent>();
        agent.Init(reconstructedNetwork, target.transform);

    }
}

[System.Serializable]
public class SavedNetwork
{
    public float fitness;
    public LayerWeightsArray[] layerArrays;
}

[System.Serializable]
public class WeightsArray
{
    public float[] weights;
}

[System.Serializable]
public class LayerWeightsArray
{
    public WeightsArray[] weightsArrays;
}