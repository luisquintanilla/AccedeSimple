import Denque from "denque";

/**
 * Unbounded channel that supports:
 * - Multiple producers writing items
 * - Consumers reading items or using for-await-of
 * - Signaling channel closed (no more items)
 * - Signaling an error (consumers see a rejected Promise)
 * - Performance-friendly, O(1) enqueue/dequeue via denque
 */
export class UnboundedChannel<T> {
  private _queue = new Denque<T>();
  // Store an array of [resolve, reject] for pending reads
  private _waitingConsumers: Array<
    [(value: IteratorResult<T>) => void, (reason?: any) => void]
  > = [];

  private _closed = false;
  private _error: Error | null = null;

  /**
   * Writes an item into the channel.
   * Throws an error if the channel is closed or errored.
   */
  public write(item: T): void {
    if (this._closed) {
      throw new Error("Cannot write to a closed channel.");
    }
    if (this._error) {
      throw new Error("Cannot write to an errored channel: " + this._error.message);
    }

    // If there's a waiting consumer, resolve it immediately with the new item
    if (this._waitingConsumers.length > 0) {
      const [resolve] = this._waitingConsumers.shift()!;
      resolve({ value: item, done: false });
    } else {
      // Otherwise, enqueue the item
      this._queue.push(item);
    }
  }

  /**
   * Reads an item from the channel as an IteratorResult<T>:
   *   { value: T, done: false } if item is available
   *   { value: undefined, done: true } if channel is closed and no items left
   * If an error has been signaled, this Promise rejects with that Error.
   */
  public read(): Promise<IteratorResult<T>> {
    // If there's an error, reject immediately
    if (this._error) {
      return Promise.reject(this._error);
    }

    // If we have items in the queue, return the next one
    if (this._queue.length > 0) {
      const value = this._queue.shift()!;
      return Promise.resolve({ value, done: false });
    }

    // If channel is closed and no items, return done
    if (this._closed) {
      return Promise.resolve({ value: undefined, done: true });
    }

    // Otherwise, wait until an item is written or an error/close occurs
    return new Promise<IteratorResult<T>>((resolve, reject) => {
      this._waitingConsumers.push([resolve, reject]);
    });
  }

  /**
   * Closes the channel. No further writes allowed.
   * All pending consumers receive { value: undefined, done: true }.
   */
  public close(): void {
    // If already closed or errored, do nothing
    if (this._closed || this._error) {
      return;
    }

    this._closed = true;

    // Resolve all waiting consumers with 'done = true'
    while (this._waitingConsumers.length > 0) {
      const [resolve] = this._waitingConsumers.shift()!;
      resolve({ value: undefined, done: true });
    }
  }

  /**
   * Signals an error. No further writes or reads.
   * All pending consumers get a rejected Promise with the given Error.
   */
  public throwError(err: Error): void {
    if (this._error || this._closed) {
      // If already in an error/closed state, do nothing or rethrow as needed
      return;
    }

    this._error = err;

    // Reject all waiting consumers
    while (this._waitingConsumers.length > 0) {
      const [, reject] = this._waitingConsumers.shift()!;
      reject(err);
    }
  }

  /**
   * Makes the channel compatible with `for await...of`.
   */
  public [Symbol.asyncIterator](): AsyncIterator<T> {
    return {
      next: () => this.read()
    };
  }
}
