﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NeoCortexApi.Entities;
using System.Linq;
using System.Diagnostics;
using NeoCortexApi.Utility;
using NeoCortexApi.DistributedCompute;

namespace NeoCortexApi
{
    public class SpatialPoolerParallel : SpatialPooler
    {
        private DistributedMemory distMemConfig;

        public override void InitMatrices(Connections c, DistributedMemory distMem)
        {
            IRemotelyDistributed remoteHtm = distMem.ColumnDictionary as IRemotelyDistributed;
            if (remoteHtm == null)
                throw new ArgumentException("");

            this.distMemConfig = distMem;

            SparseObjectMatrix<Column> mem = (SparseObjectMatrix<Column>)c.getMemory();

            c.setMemory(mem == null ? mem = new SparseObjectMatrix<Column>(c.getColumnDimensions(), dict : distMem.ColumnDictionary) : mem);

            c.setInputMatrix(new SparseBinaryMatrix(c.getInputDimensions()));

            // Initiate the topologies
            c.setColumnTopology(new Topology(c.getColumnDimensions()));
            c.setInputTopology(new Topology(c.getInputDimensions()));           

            //Calculate numInputs and numColumns
            int numInputs = c.getInputMatrix().getMaxIndex() + 1;
            int numColumns = c.getMemory().getMaxIndex() + 1;
            if (numColumns <= 0)
            {
                throw new ArgumentException("Invalid number of columns: " + numColumns);
            }
            if (numInputs <= 0)
            {
                throw new ArgumentException("Invalid number of inputs: " + numInputs);
            }
            c.NumInputs = numInputs;
            c.setNumColumns(numColumns);
            
            if (distMem != null)
            {
                var distHtmCla = distMem.ColumnDictionary as HtmSparseIntDictionary<Column>;
                distHtmCla.HtmConfig = c.HtmConfig;
            }

            //
            // Fill the sparse matrix with column objects
            var numCells = c.getCellsPerColumn();

            var partitions = mem.GetPartitions();


            List<KeyPair> colList = new List<KeyPair>();
            for (int i = 0; i < numColumns; i++)
            {
                //colList.Add(new KeyPair() { Key = i, Value = new Column(numCells, i, c.getSynPermConnected(), c.NumInputs) });
                colList.Add(new KeyPair() { Key = i, Value = c.HtmConfig });
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            remoteHtm.InitializeColumnPartitionsDist(colList);

            //mem.set(colList);

            sw.Stop();
            //c.setPotentialPools(new SparseObjectMatrix<Pool>(c.getMemory().getDimensions(), dict: distMem == null ? null : distMem.PoolDictionary));

            Debug.WriteLine($" Upload time: {sw.ElapsedMilliseconds}");

            // Already initialized by creating of columns.
            //c.setConnectedMatrix(new SparseBinaryMatrix(new int[] { numColumns, numInputs }));

            //Initialize state meta-management statistics
            c.setOverlapDutyCycles(new double[numColumns]);
            c.setActiveDutyCycles(new double[numColumns]);
            c.setMinOverlapDutyCycles(new double[numColumns]);
            c.setMinActiveDutyCycles(new double[numColumns]);
            c.BoostFactors = (new double[numColumns]);
            ArrayUtils.fillArray(c.BoostFactors, 1);
        }

        /// <summary>
        /// Implements muticore initialization of pooler.
        /// </summary>
        /// <param name="c"></param>
        protected override void ConnectAndConfigureInputs(Connections c)
        {
            IRemotelyDistributed remoteHtm = this.distMemConfig.ColumnDictionary as IRemotelyDistributed;
            if (remoteHtm == null)
                throw new ArgumentException("");

            List<double> avgSynapsesConnected = remoteHtm.ConnectAndConfigureInputsDist(c.HtmConfig);


            //List<KeyPair> colList = new List<KeyPair>();

            //ConcurrentDictionary<int, KeyPair> colList2 = new ConcurrentDictionary<int, KeyPair>();

            //int numColumns = c.NumColumns;

            //// Parallel implementation of initialization
            //ParallelOptions opts = new ParallelOptions();
            ////int synapseCounter = 0;



            //Parallel.For(0, numColumns, opts, (indx) =>
            //{

            //    //Random rnd = new Random(42);

            //    //int i = (int)indx;
            //    //var data = new ProcessingDataParallel();

            //    //// Gets RF
            //    //data.Potential = HtmCompute.MapPotential(c.HtmConfig, i, rnd);
            //    //data.Column = c.getColumn(i);

            //    //// This line initializes all synases in the potential pool of synapses.
            //    //// It creates the pool on proximal dendrite segment of the column.
            //    //// After initialization permancences are set to zero.
            //    ////connectColumnToInputRF(c.HtmConfig, data.Potential, data.Column);
            //    //data.Column.CreatePotentialPool(c.HtmConfig, data.Potential, -1);

            //    ////Interlocked.Add(ref synapseCounter, data.Column.ProximalDendrite.Synapses.Count);

            //    ////colList.Add(new KeyPair() { Key = i, Value = column });

            //    //data.Perm = HtmCompute.InitSynapsePermanences(c.HtmConfig, data.Potential, rnd);

            //    //data.AvgConnected = GetAvgSpanOfConnectedSynapses(c, i);

            //    //HtmCompute.UpdatePermanencesForColumn(c.HtmConfig, data.Perm, data.Column, data.Potential, true);

            //    if (!colList2.TryAdd(i, new KeyPair() { Key = i, Value = data }))
            //    {

            //    }
            //});

            ////c.setProximalSynapseCount(synapseCounter);

            //List<double> avgSynapsesConnected = new List<double>();

            //foreach (var item in colList2.Values)
            ////for (int i = 0; i < numColumns; i++)
            //{
            //    int i = (int)item.Key;

            //    ProcessingDataParallel data = (ProcessingDataParallel)item.Value;
            //    //ProcessingData data = new ProcessingData();

            //    // Debug.WriteLine(i);
            //    //data.Potential = mapPotential(c, i, c.isWrapAround());

            //    //var st = string.Join(",", data.Potential);
            //    //Debug.WriteLine($"{i} - [{st}]");

            //    //var counts = c.getConnectedCounts();

            //    //for (int h = 0; h < counts.getDimensions()[0]; h++)
            //    //{
            //    //    // Gets the synapse mapping between column-i with input vector.
            //    //    int[] slice = (int[])counts.getSlice(h);
            //    //    Debug.Write($"{slice.Count(y => y == 1)} - ");
            //    //}
            //    //Debug.WriteLine(" --- ");
            //    // Console.WriteLine($"{i} - [{String.Join(",", ((ProcessingData)item.Value).Potential)}]");

            //    // This line initializes all synases in the potential pool of synapses.
            //    // It creates the pool on proximal dendrite segment of the column.
            //    // After initialization permancences are set to zero.
            //    //var potPool = data.Column.createPotentialPool(c, data.Potential);
            //    //connectColumnToInputRF(c, data.Potential, data.Column);

            //    //data.Perm = initPermanence(c.getSynPermConnected(), c.getSynPermMax(),
            //    //      c.getRandom(), c.getSynPermTrimThreshold(), c, data.Potential, data.Column, c.getInitConnectedPct());

            //    //updatePermanencesForColumn(c, data.Perm, data.Column, data.Potential, true);

            //    avgSynapsesConnected.Add(data.AvgConnected);

            //    colList.Add(new KeyPair() { Key = i, Value = data.Column });
            //}

            SparseObjectMatrix<Column> mem = (SparseObjectMatrix<Column>)c.getMemory();

            //if (mem.IsRemotelyDistributed)
            //{
            //    // Pool is created and attached to the local instance of Column.
            //    // Here we need to update the pool on remote Column instance.
            //    mem.set(colList);
            //}

            // The inhibition radius determines the size of a column's local
            // neighborhood.  A cortical column must overcome the overlap score of
            // columns in its neighborhood in order to become active. This radius is
            // updated every learning round. It grows and shrinks with the average
            // number of connected synapses per column.
            updateInhibitionRadius(c, avgSynapsesConnected);
        }

        /*
        /// <summary>
        /// Implements single threaded (originally based on JAVA implementation) initialization of SP.
        /// </summary>
        /// <param name="c"></param>
        protected override void ConnectAndConfigureInputs(Connections c)
        {
            List<KeyPair> colList = new List<KeyPair>();
            ConcurrentDictionary<int, KeyPair> colList2 = new ConcurrentDictionary<int, KeyPair>();

            int numColumns = c.NumColumns;

            // We need dictionary , which implements Akk paralellism.
            HtmSparseIntDictionary<Column> colDict = this.distMemConfig.ColumnDictionary as HtmSparseIntDictionary<Column>;
            if (colDict == null)
                throw new ArgumentException($"ColumnDictionary must be of type {nameof(HtmSparseIntDictionary<Column>)}!");

            // Parallel implementation of initialization
            ParallelOptions opts = new ParallelOptions();
            opts.MaxDegreeOfParallelism = colDict.Nodes;

            //int synapseCounter = 0;

            SparseObjectMatrix<Column> mem = (SparseObjectMatrix<Column>)c.getMemory();
            
            if (mem.IsRemotelyDistributed == false)
                throw new ArgumentException("Column memory matrix 'SparseObjectMatrix<Column>' must be remotely distributed.");

            var partitions = mem.GetPartitions();
            
            Parallel.ForEach(partitions, opts, (keyValPair) =>
            {
                //// We get here keys grouped to actors, which host partitions.
                //var partitions = mem.GetPartitionsForKeyset(keyValuePairs);

                ////int i = keyValPair.Key;

                ////mem.GetObjects(keyValPair.ToArray());

                //var data = new ProcessingData();

               
                //// Gets RF
                //data.Potential = mapPotential(c, i, c.isWrapAround());

                //mem.GetObjects(keyValPair.Value.ToArray());

                //data.Column = c.getColumn(i);

                //Parallel.ForEach(pages, opts, (keyValPair) =>
                //{

                
                //    // This line initializes all synases in the potential pool of synapses.
                //    // It creates the pool on proximal dendrite segment of the column.
                //    // After initialization permancences are set to zero.
                //    connectColumnToInputRF(c, data.Potential, data.Column);

                ////Interlocked.Add(ref synapseCounter, data.Column.ProximalDendrite.Synapses.Count);

                ////colList.Add(new KeyPair() { Key = i, Value = column });

                //data.Perm = initSynapsePermanencesForColumn(c, data.Potential, data.Column);

                //updatePermanencesForColumn(c, data.Perm, data.Column, data.Potential, true);

                //if (!colList2.TryAdd(i, new KeyPair() { Key = i, Value = data }))
                //{

                //}
                //});
            });

            //c.setProximalSynapseCount(synapseCounter);

            foreach (var item in colList2.Values)
            //for (int i = 0; i < numColumns; i++)
            {
                int i = (int)item.Key;

                ProcessingData data = (ProcessingData)item.Value;
                //ProcessingData data = new ProcessingData();

                // Debug.WriteLine(i);
                //data.Potential = mapPotential(c, i, c.isWrapAround());

                //var st = string.Join(",", data.Potential);
                //Debug.WriteLine($"{i} - [{st}]");

                //var counts = c.getConnectedCounts();

                //for (int h = 0; h < counts.getDimensions()[0]; h++)
                //{
                //    // Gets the synapse mapping between column-i with input vector.
                //    int[] slice = (int[])counts.getSlice(h);
                //    Debug.Write($"{slice.Count(y => y == 1)} - ");
                //}
                //Debug.WriteLine(" --- ");
                // Console.WriteLine($"{i} - [{String.Join(",", ((ProcessingData)item.Value).Potential)}]");

                // This line initializes all synases in the potential pool of synapses.
                // It creates the pool on proximal dendrite segment of the column.
                // After initialization permancences are set to zero.
                //var potPool = data.Column.createPotentialPool(c, data.Potential);
                //connectColumnToInputRF(c, data.Potential, data.Column);

                //data.Perm = initPermanence(c.getSynPermConnected(), c.getSynPermMax(),
                //      c.getRandom(), c.getSynPermTrimThreshold(), c, data.Potential, data.Column, c.getInitConnectedPct());

                //updatePermanencesForColumn(c, data.Perm, data.Column, data.Potential, true);

                colList.Add(new KeyPair() { Key = i, Value = data.Column });
            }


            if (mem.IsRemotelyDistributed)
            {
                // Pool is created and attached to the local instance of Column.
                // Here we need to update the pool on remote Column instance.
                mem.set(colList);
            }

            // The inhibition radius determines the size of a column's local
            // neighborhood.  A cortical column must overcome the overlap score of
            // columns in its neighborhood in order to become active. This radius is
            // updated every learning round. It grows and shrinks with the average
            // number of connected synapses per column.
            updateInhibitionRadius(c);
        }
        */



        /// <summary>
        /// Starts distributed calculation of overlaps.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="inputVector">Overlap of every column.</param>
        /// <returns></returns>
        public override int[] CalculateOverlap(Connections c, int[] inputVector)
        {
            IRemotelyDistributed remoteHtm = this.distMemConfig.ColumnDictionary as IRemotelyDistributed;
            if (remoteHtm == null)
                throw new ArgumentException("disMemConfig is not of type IRemotelyDistributed!");

            //c.getColumn(0).GetColumnOverlapp(inputVector, c.StimulusThreshold);

            int[] columnOverlaps = remoteHtm.CalculateOverlapDist(inputVector);

            return columnOverlaps;
        }

        public override void AdaptSynapses(Connections c, int[] inputVector, int[] activeColumns)
        {
            throw new NotImplementedException();

            // Get all indicies of input vector, which are set on '1'.
            var inputIndices = ArrayUtils.IndexWhere(inputVector, inpBit => inpBit > 0);

            double[] permChanges = new double[c.NumInputs];

            // First we initialize all permChanges to minimum decrement values,
            // which are used in a case of none-connections to input.
            ArrayUtils.fillArray(permChanges, -1 * c.getSynPermInactiveDec());

            // Then we update all connected permChanges to increment values for connected values.
            // Permanences are set in conencted input bits to default incremental value.

            ArrayUtils.setIndexesTo(permChanges, inputIndices.ToArray(), c.getSynPermActiveInc());
            for (int i = 0; i < activeColumns.Length; i++)
            {
                //Pool pool = c.getPotentialPools().get(activeColumns[i]);
                Pool pool = c.getColumn(activeColumns[i]).ProximalDendrite.RFPool;
                double[] perm = pool.getDensePermanences(c);
                int[] indexes = pool.getSparsePotential();
                ArrayUtils.raiseValuesBy(permChanges, perm);
                Column col = c.getColumn(activeColumns[i]);
                HtmCompute.UpdatePermanencesForColumn(c.HtmConfig, perm, col, indexes, true);
            }

            //Debug.WriteLine("Permance after update in adaptSynapses: " + permChangesStr);
        }

        class ProcessingDataParallel
        {
            public int[] Potential { get; set; }

            public Column Column { get; set; }

            public double[] Perm { get; internal set; }

            public double AvgConnected { get; set; }
        }
    }
}
