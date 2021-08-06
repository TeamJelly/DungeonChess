﻿using System.Collections;
using UnityEngine;
using Model.Managers;
using System.Collections.Generic;

namespace Common
{
    public class PathFind
    {
        class Node
        {
            public Vector2Int unitPosition;
            public Vector2Int destination;
            public Node parent;
            public int heuristicCost;
            public int fromCost;
            public int evaluationCost;

            public Node(Vector2Int unitPosition, Vector2Int destination) // root 초기화
            {
                this.unitPosition = unitPosition;
                this.destination = destination;
                parent = null;
                heuristicCost = Heuristic(unitPosition, destination);
                fromCost = 0;
                evaluationCost = heuristicCost + fromCost;
            }

            public Node(Vector2Int unitPosition, Node parent)
            {
                this.unitPosition = unitPosition;
                this.parent = parent;
                destination = parent.destination;
                heuristicCost = Heuristic(unitPosition, destination);
                fromCost = parent.fromCost + 1;
                evaluationCost = heuristicCost + fromCost;
            }

            public static Node PopSmallestCostNode(List<Node> nodeList)
            {
                if (nodeList.Count == 0)
                    return null;

                Node temp = nodeList[0];

                foreach (var item in nodeList)
                    if (temp.evaluationCost > item.evaluationCost)
                        temp = item;

                return temp;
            }

            public static List<Node> GetAvilableNeighbor(Model.Unit agent, Node node)
            {
                List<Node> neighbor = new List<Node>();

                Vector2Int[] UDLR = {Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right};
                Vector2Int[] diagonals = {
                    Vector2Int.up + Vector2Int.left, Vector2Int.up + Vector2Int.right,
                    Vector2Int.down + Vector2Int.left, Vector2Int.down + Vector2Int.right
                };

                List<Vector2Int> positions = new List<Vector2Int>();

                foreach (Vector2Int vector in UDLR)
                    positions.Add(node.unitPosition + vector);

                foreach (Vector2Int vector in diagonals)
                    positions.Add(node.unitPosition + vector);

                foreach (Vector2Int position in positions)
                    if (FieldManager.IsInField(position) && FieldManager.GetTile(position).IsPositionable(agent))
                        neighbor.Add(new Node(position, node));

                return neighbor;
            }

            public static List<Vector2Int> RebuildPath(Node current)
            {
                List<Vector2Int> totalPath = new List<Vector2Int>();
                totalPath.Add(current.unitPosition);

                while (current.parent != null)
                {
                    current = current.parent;
                    totalPath.Add(current.unitPosition);
                }
                totalPath.Reverse();

                return totalPath;
            }
            static int Heuristic(Vector2Int from, Vector2Int to)
            {
                Vector2Int temp = from - to;
                return Mathf.Abs(temp.x) + Mathf.Abs(temp.y);
            }
        }

        /// <summary>
        /// 유닛의 이동경로를 찾는 알고리즘.
        /// </summary>
        /// <param name="unit">이동 유닛</param>
        /// <param name="from">출발 위치</param>
        /// <param name="to">도착 위치</param>
        /// <returns></returns>
        public static List<Vector2Int> PathFindAlgorithm(Model.Unit agent, Vector2Int from, Vector2Int to)
        {
            if (FieldManager.GetTile(to) == null || FieldManager.GetTile(to).HasUnit())
            {
                Debug.LogWarning("길찾기 알고리즘 오류");
                return null;
            }

            Node node = new Node(from, to);
            List<Node> frontier = new List<Node>(); // priority queue ordered by Path-Cost, with node as the only element
            List<Node> explored = new List<Node>(); // an empty set

            frontier.Add(node);

            while (true)
            {
                if (frontier.Count == 0)
                    return null; // 답이 없음.

                node = Node.PopSmallestCostNode(frontier);
                frontier.Remove(node);

                if (node.unitPosition.Equals(to)) // goal test
                    return Node.RebuildPath(node);

                explored.Add(node); // add node.State to explored

                foreach (var child in Node.GetAvilableNeighbor(agent, node))
                {
                    bool isExplored = false;
                    foreach (var item in explored)
                        if (item.unitPosition == child.unitPosition)
                            isExplored = true;
                    if (isExplored.Equals(true))
                        continue;

                    bool isFrontiered = false;

                    for (int i = frontier.Count -1; i >= 0; i--)
                        if (frontier[i].unitPosition.Equals(child.unitPosition))
                        {
                            isFrontiered = true;
                            if (child.unitPosition == frontier[i].unitPosition && 
                                child.evaluationCost < frontier[i].evaluationCost)
                            {
                                frontier.Remove(frontier[i]);
                                frontier.Add(child);
                            }
                        }

                    if (isFrontiered.Equals(false))
                        frontier.Add(child);
                }
            }
        }
    }
}