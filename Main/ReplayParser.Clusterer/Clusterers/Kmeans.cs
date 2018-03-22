using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.Clusterer.BuildorderTree;
using ReplayParser.Actions;

namespace ReplayParser.Clusterer
{
    class Centroid
    {
        private Node<BuildAction> m_centroid;
        public Node<BuildAction> Value { get { return m_centroid; } }

        private List<Node<BuildAction>> m_observations = new List<Node<BuildAction>>();
        public List<Node<BuildAction>> Observations { get { return m_observations; } }

        public Centroid(Node<BuildAction> centroid)
        {
            this.m_centroid = centroid;
        }

        public void AddObservation(Node<BuildAction> obs)
        {
            m_observations.Add(obs);
        }
    }

    class Kmeans
    {
        private static Random rand = new Random();
        private List<Centroid> m_clusters;
        public List<Centroid> Clusters { get { return m_clusters; } }

        public void Cluster(int k, NodeList<BuildAction> observations)
        {
             
            // Use random observations as centroids
            //List<Centroid> centroids = initialCentroidRandom(k, observations);
            List<Centroid> centroids = initialCentroidReasonable(observations); // OBS! Ignores k
            foreach (Centroid c in centroids)
                observations.Remove(c.Value);

            assignToCentroid(observations, centroids);

            // TODO: Check if stability has occured instead
            // Im tired, no moar coffee....
            for (int i = 0; i < 3; i++)
            {
                centroids = iterate(centroids);
                assignToCentroid(observations, centroids);
            }

            foreach (var c in centroids)
            {
                var err = c.Observations.Where(x => x.Value.ObjectType != c.Value.Value.ObjectType);
                System.Console.WriteLine("Error count: " + err.Count());
            }

            m_clusters = centroids;
        }

        private List<Centroid> initialCentroidReasonable(NodeList<BuildAction> observations)
        {
            List<Centroid> centroids = new List<Centroid>();

            // We know there are 6 possible openers in Starcraft (based on datamining).
            // Use these as centroids
            centroids.Add(new Centroid(observations.Where(x => x.Value.ObjectType == Entities.ObjectType.Pylon).First()));
            centroids.Add(new Centroid(observations.Where(x => x.Value.ObjectType == Entities.ObjectType.Extractor).First()));
            centroids.Add(new Centroid(observations.Where(x => x.Value.ObjectType == Entities.ObjectType.SpawningPool).First()));
            centroids.Add(new Centroid(observations.Where(x => x.Value.ObjectType == Entities.ObjectType.Hatchery).First()));
            centroids.Add(new Centroid(observations.Where(x => x.Value.ObjectType == Entities.ObjectType.Barracks).First()));
            centroids.Add(new Centroid(observations.Where(x => x.Value.ObjectType == Entities.ObjectType.SupplyDepot).First()));

            return centroids;
        }

        private List<Centroid> initialCentroidRandom(int k, NodeList<BuildAction> observations)
        {
            List<Centroid> centroids = new List<Centroid>();
            while (k > 0)
            {
                centroids.Add(new Centroid(observations.ElementAt(rand.Next(observations.Count - 1))));
                k--;
            }
            return centroids;
        }

        private void assignToCentroid(NodeList<BuildAction> observations, List<Centroid> centroids)
        {
            // Alternative implementation of the following for-loops. Completely unreadable (also slower?), but cool!
            //var stuffz = observations.Select(x => centroids.Select(y => new { y, distance = calcDistance(x, y.Value) }).OrderBy(z => z.distance).First());

            foreach (var o in observations)
            {
                Centroid closestCentroid = null;
                double closestDistance = 9999999999999999999;
                foreach (var c in centroids)
                {
                    double dist = calcDistance(o, c.Value);
                    if (dist < closestDistance)
                    {
                        closestCentroid = c;
                        closestDistance = dist;
                    }

                }
                closestCentroid.AddObservation(o);
            }
        }

        private List<Centroid> iterate(List<Centroid> centroids)
        {
            List<Centroid> result = new List<Centroid>();

            foreach (var c in centroids)
            {
                Centroid newCentroid = generateCentroid(c.Observations);
                if (newCentroid != null)
                    result.Add(newCentroid);
                else
                    result.Add(c);
            }
            return result;
        }

        private Centroid generateCentroid(List<Node<BuildAction>> observations)
        {
            if (observations.Count == 0) return null;
            // Find most common building
            var mostCommonBuilding = (from item in observations
                          group item by item.Value.ObjectType into g
                          orderby g.Count() descending
                          select g.Key).First();

            var commonObs = observations.Where(x => x.Value.ObjectType.ToString() == mostCommonBuilding.ToString());
            
            Node<BuildAction> node = new Node<BuildAction>(1, commonObs.First().Value, generateCentroidNeighbours(commonObs));
            
            return new Centroid(node);
        }

        private NodeList<BuildAction> generateCentroidNeighbours(IEnumerable<Node<BuildAction>> commonObs)
        {
            // Ensure we actually have neighbours to add
            if (commonObs == null) return null;
            foreach (var n in commonObs)
            {
                if (n.Neighbors == null) return null;
            }

            // Find the most common building among neighbours
            var mostCommonNeighbourBuilding = (from item in commonObs.SelectMany(x => x.Neighbors)
                                   group item by item.Value.ObjectType into g
                                   orderby g.Count() descending
                                   select g.Key).First();

            var commonNeighbour = commonObs.SelectMany(x => x.Neighbors)
                .Where(x => x.Value.ObjectType.ToString() == mostCommonNeighbourBuilding.ToString());

            NodeList<BuildAction> result = new NodeList<BuildAction>();
            result.Add(new Node<BuildAction>(1, commonNeighbour.First().Value, generateCentroidNeighbours(commonNeighbour)));
            return result;
        }

        

        private double calcDistance(Node<BuildAction> o, Node<BuildAction> c, int heightCounter = 1)
        {
            double result = 0;

            if (o.Value.ObjectType != c.Value.ObjectType)
                result += weight(heightCounter);

            if (o.Neighbors == null || c.Neighbors == null) 
                return result+(1*(1/heightCounter)); // "Punish" observations with low height
            // You are supposed to pass in the raw 'games' themself, not the complete, built, tree. Thus, every node will have at most 1 child.
            result += calcDistance(o.Neighbors.ElementAt(0), c.Neighbors.ElementAt(0), ++heightCounter);
            return result;
        }

        private double weight(int n)
        {
            // Exponentially decaying, n > 20 = 0. Visuals: http://www.wolframalpha.com/input/?i=lim+e^%28-n%2F5%29+as+n-%3E10
            return 1000*(Math.Pow(Math.E, (-n)));
        }       
    }
}
