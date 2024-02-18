using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmalgamGames.SceneManagement
{
    public static class PhysicsSimulation
    {
        public static SimulatedScene CreateSimulatedScene(GameObject simulatedObject, GameObject[] collisionObjects)
        {
            CreateSceneParameters sceneParameters = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
            Scene simScene = SceneManager.CreateScene("SimScene", sceneParameters);
            PhysicsScene physicsScene = simScene.GetPhysicsScene();

            GameObject newSimulatedObject = GameObject.Instantiate(simulatedObject);
            SceneManager.MoveGameObjectToScene(newSimulatedObject, simScene);

            foreach(GameObject obj in collisionObjects)
            {
                GameObject newObj = GameObject.Instantiate(obj, obj.transform.position, obj.transform.rotation);
                SceneManager.MoveGameObjectToScene(newObj, simScene);
            }

            SimulatedScene finalScene = new SimulatedScene(newSimulatedObject,simScene, physicsScene);

            return finalScene;
        }
    }

    public class SimulatedScene
    {
        public GameObject simulatedObject;
        public Scene simulatedScene;
        public PhysicsScene simulatedPhysicsScene;

        public SimulatedScene(GameObject simulatedObject, Scene simulatedScene, PhysicsScene simulatedPhysicsScene)
        {
            this.simulatedObject = simulatedObject;
            this.simulatedScene = simulatedScene;
            this.simulatedPhysicsScene = simulatedPhysicsScene;
        }
    }
}