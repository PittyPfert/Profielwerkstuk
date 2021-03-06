using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Profielwerkstuk
{
    public class TaskManager
    {
        public List<Vector3> Tasks;
        public Vector3 waitingForRegisterPos;
        public Vector3 leavingPos;  


        private NavMeshAgent agent;

        public TaskManager(NavMeshAgent _agent)
        {
            agent = _agent;
            Tasks = new List<Vector3>();
        }

        private bool CanReach(Vector3 point)
        {
            NavMeshPath path = new NavMeshPath();
            return agent.CalculatePath(point, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        private float GetPathDistance(NavMeshPath path)
        {
            float lng = 0.0f;
            // Debug.Log(path.status);
            //Debug.Log(path.corners.Length);
            if ((path.status != NavMeshPathStatus.PathInvalid) && (path.corners.Length > 1))
            {
                // Debug.Log("Calculating Distance!");
                for (int i = 1; i < path.corners.Length; i++)
                {
                    lng += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                }
            }

            return lng;
        }

        public void RemoveTask(Vector3 toRemove)
        {
            Tasks.Remove(toRemove);
        }

        public bool GetTask(out Vector3 target)
        {

            // Debug.Log("There are " + Tasks.Count + " tasks left.");
            target = Vector3.positiveInfinity;
            NavMeshPath path = new NavMeshPath();
            float minDistance = -1;
            // Debug.Log(minDistance);
            for (int i = 0; i < Tasks.Count; i++)
            {
                if (!CanReach(Tasks[i])) continue;
                agent.CalculatePath(Tasks[i], path);
                float distance = GetPathDistance(path);
                // Debug.Log(distance); 
                if (distance < minDistance || minDistance < 0)
                {
                    // Debug.Log("Distance was altered");
                    minDistance = distance;
                    target = Tasks[i];
                }
            }
            if(!(minDistance > 0)) {
                Debug.Log("I've fallen and I can't up");
            }
            return minDistance > 0;
        }
        private Vector3 GetPos(Transform area)
        {
            float minX = area.position.x - area.localScale.x / 2;
            float maxX = area.position.x + area.localScale.x / 2;
            float minZ = area.position.z - area.localScale.z / 2;
            float maxZ = area.position.z + area.localScale.z / 2;
            float y = area.position.y + area.localScale.y / 2;

            Vector3 target;
            NavMeshHit hit;
            do {
                float x = Random.Range(minX, maxX);
                float z = Random.Range(minZ, maxZ);
                target = new Vector3(x, y, z);
            } while(!NavMesh.SamplePosition(target, out hit, 2f, NavMesh.AllAreas));
            return hit.position;
        }

        public void SetLeavingPos(Transform area)
        {
            leavingPos = GetPos(area);
        }

        public void AddTask(Vector3 toAdd)
        {
            if (!Tasks.Contains(toAdd)) Tasks.Add(toAdd);
        }

    }
}
