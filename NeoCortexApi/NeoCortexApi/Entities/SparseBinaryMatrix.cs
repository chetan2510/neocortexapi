﻿using NeoCortexApi.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeoCortexApi.Entities
{
    /**
 * Implementation of a sparse matrix which contains binary integer
 * values only.
 * 
 * @author cogmission
 *
 */
    public class SparseBinaryMatrix : AbstractSparseBinaryMatrix
    {
        /** keep it simple */
        private static readonly long serialVersionUID = 1L;

        private Array backingArray;

        /**
         * Constructs a new {@code SparseBinaryMatrix} with the specified
         * dimensions (defaults to row major ordering)
         * 
         * @param dimensions    each indexed value is a dimension size
         */
        public SparseBinaryMatrix(int[] dimensions) : this(dimensions, false)
        {

        }

        /**
         * Constructs a new {@code SparseBinaryMatrix} with the specified dimensions,
         * allowing the specification of column major ordering if desired. 
         * (defaults to row major ordering)
         * 
         * @param dimensions                each indexed value is a dimension size
         * @param useColumnMajorOrdering    if true, indicates column first iteration, otherwise
         *                                  row first iteration is the default (if false).
         */
        public SparseBinaryMatrix(int[] dimensions, bool useColumnMajorOrdering) : base(dimensions, useColumnMajorOrdering)
        {
            this.backingArray = Array.CreateInstance(typeof(int), dimensions);
        }

        /**
         * Called during mutation operations to simultaneously set the value
         * on the backing array dynamically.
         * @param val
         * @param coordinates
         */
        private void back(int val, params int[] coordinates)
        {
            //update true counts
            ArrayUtils.setValue(this.backingArray, val, coordinates);
            int aggVal = -1;
            //int aggregateVal = ArrayUtils.aggregateArray(((System.Int32[,])this.backingArray)[coordinates[0],1]);
            if (this.backingArray is System.Int32[,])
            {
                var row = ArrayUtils.GetRow<Int32>((System.Int32[,])this.backingArray, 0);
                aggVal = ArrayUtils.aggregateArray(row);
            }
            else
                throw new NotSupportedException();

            setTrueCount(coordinates[0], aggVal);
            // setTrueCount(coordinates[0], ArrayUtils.aggregateArray(((Object[])this.backingArray)[coordinates[0]]));
        }

        /**
         * Returns the slice specified by the passed in coordinates.
         * The array is returned as an object, therefore it is the caller's
         * responsibility to cast the array to the appropriate dimensions.
         * 
         * @param coordinates	the coordinates which specify the returned array
         * @return	the array specified
         * @throws	IllegalArgumentException if the specified coordinates address
         * 			an actual value instead of the array holding it.
         */
       // @Override

        public override Object getSlice(params int[] coordinates)
        {
            //Object slice = ArrayUtils.getValue(this.backingArray, coordinates);
            Object slice;
            if (coordinates.Length == 1)
                slice = ArrayUtils.GetRow<int>((int[,])this.backingArray, coordinates[0]);
            //else if (coordinates.Length == 1)
            //    slice = ((int[])this.backingArray)[coordinates[0]];
            else
                throw new ArgumentException();

                //Ensure return value is of type Array
                if (!slice.GetType().IsArray)
            {
                sliceError(coordinates);
            }

            return slice;
        }

        /**
         * Fills the specified results array with the result of the 
         * matrix vector multiplication.
         * 
         * @param inputVector		the right side vector
         * @param results			the results array
         */
        public override void rightVecSumAtNZ(int[] inputVector, int[] results)
        {
            for (int i = 0; i < dimensions[0]; i++)
            {
                int[] slice = (int[])(dimensions.Length > 1 ? getSlice(i) : backingArray);
                for (int j = 0; j < slice.Length; j++)
                {
                    results[i] += (inputVector[j] * slice[j]);
                }
            }
        }

        /**
         * Fills the specified results array with the result of the 
         * matrix vector multiplication.
         * 
         * @param inputVector       the right side vector
         * @param results           the results array
         */
        public override void rightVecSumAtNZ(int[] inputVector, int[] results, double stimulusThreshold)
        {
            for (int i = 0; i < dimensions[0]; i++)
            {
                int[] slice = (int[])(dimensions.Length > 1 ? getSlice(i) : backingArray);
                for (int j = 0; j < slice.Length; j++)
                {
                    results[i] += (inputVector[j] * slice[j]);
                    if (j == slice.Length - 1)
                    {
                        // If the stimulus is 0 then results[i] is never less than 0 and it remains unchanged.
                        // If the stimulus is > 0 then results[i] is also never less then results[i]. This is strange.
                        results[i] -= results[i] < stimulusThreshold ? results[i] : 0;
                    }
                }
            }
        }

        /**
         * Sets the value at the specified index.
         * 
         * @param index     the index the object will occupy
         * @param object    the object to be indexed.
         */
       // @Override
    public  AbstractSparseBinaryMatrix set(int index, int value)
        {
            int[] coordinates = computeCoordinates(index);
            return set(value, coordinates);
        }

        /**
         * Sets the value to be indexed at the index
         * computed from the specified coordinates.
         * @param coordinates   the row major coordinates [outer --> ,...,..., inner]
         * @param object        the object to be indexed.
         */
        //@Override
    public override AbstractSparseBinaryMatrix set(int value, params int[] coordinates)
        {
            back(value, coordinates);
            return this;
        }

        /**
         * Sets the specified values at the specified indexes.
         * 
         * @param indexes   indexes of the values to be set
         * @param values    the values to be indexed.
         * 
         * @return this {@code SparseMatrix} implementation
         */
        public AbstractSparseBinaryMatrix set(int[] indexes, int[] values)
        {
            for (int i = 0; i < indexes.Length; i++)
            {
                set(indexes[i], values[i]);
            }
            return this;
        }

        /**
         * Clears the true counts prior to a cycle where they're
         * being set
         */
        public override void clearStatistics(int row)
        {
            if (backingArray.Rank != 2)
                throw new InvalidOperationException("Currently supported 2D arrays only");

            var cols = backingArray.GetLength(1);
            for (int i = 0; i < cols; i++)
            {
                backingArray.SetValue(0, row, i);
            }

            this.setTrueCount(row, 0);
            //int[] slice = (int[])backingArray.GetValue(row);
            //    int[] slice = (int[])Array.get(backingArray, row);
            //ArrayUtils.Fill(slice, 0);
            // Array.fill(slice, 0);
        }

  


      //  @Override
    public override AbstractSparseBinaryMatrix setForTest(int index, int value)
        {
            ArrayUtils.setValue(this.backingArray, value, computeCoordinates(index));
            return this;
        }
             

        //public override Integer Get(int index)
        //{
        //    return (Integer)get(index);
        //}


        // @Override
        public override Integer get(int index)
        {
            int[] coordinates = computeCoordinates(index);
            if (coordinates.Length == 1)
            {
                return (Integer)backingArray.GetValue(index);
                // return Array.getInt(this.backingArray, index);
            }

            else return (Integer)ArrayUtils.getValue(this.backingArray, coordinates);
        }

        //public override AbstractFlatMatrix<Integer> set(int index, Integer value)
        //{
        //    throw new NotImplementedException();
        //}
        public AbstractSparseBinaryMatrix set(int index, Object value)
        {
            set(index, ((Integer)value).Value);
            return this;
        }

        public override AbstractFlatMatrix<Integer> set(int index, Integer value)
        {
            throw new NotImplementedException();
        }

       
    }
}