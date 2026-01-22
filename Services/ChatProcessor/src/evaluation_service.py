"""
Offline Evaluation Service using Ragas

This module provides functionality to evaluate RAG responses using the Ragas framework.
It reads logged chat interactions, evaluates them using faithfulness and answer_relevancy metrics,
and saves the scored results to a new file.
"""
import json
import os
from typing import List, Dict, Any, Optional
from pathlib import Path
from datasets import Dataset
from ragas import evaluate
from ragas.metrics import faithfulness, answer_relevancy
from src.logger import logger
from src.config import settings


class EvaluationService:
    """
    Service for batch evaluation of RAG responses using Ragas framework.
    """

    def __init__(self, input_file: str = "chat_logs.json", output_file: str = "chat_logs_scored.json"):
        """
        Initialize the evaluation service.

        Args:
            input_file: Path to the input JSON file containing chat logs
            output_file: Path to the output JSON file for scored results
        """
        self.input_file = Path(input_file)
        self.output_file = Path(output_file)

    def load_logs(self) -> List[Dict[str, Any]]:
        """
        Load chat logs from the input JSON file.

        Returns:
            List of chat log entries

        Raises:
            FileNotFoundError: If the input file doesn't exist
            json.JSONDecodeError: If the file contains invalid JSON
        """
        if not self.input_file.exists():
            raise FileNotFoundError(f"Input file not found: {self.input_file}")

        try:
            with open(self.input_file, 'r', encoding='utf-8') as f:
                logs = json.load(f)
                logger.info(f"Loaded {len(logs)} entries from {self.input_file}")
                return logs
        except json.JSONDecodeError as e:
            logger.error(f"Invalid JSON in {self.input_file}: {e}")
            raise

    def filter_unevaluated_logs(self, logs: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        """
        Filter logs to find entries where ragas_score is missing or null.

        Args:
            logs: List of all log entries

        Returns:
            List of unevaluated log entries
        """
        unevaluated = [
            log for log in logs
            if log.get("ragas_score") is None or "ragas_score" not in log
        ]
        logger.info(f"Found {len(unevaluated)} unevaluated entries out of {len(logs)} total")
        return unevaluated

    def convert_to_dataset(self, logs: List[Dict[str, Any]]) -> Dataset:
        """
        Convert chat logs to HuggingFace Dataset format required by Ragas.

        Args:
            logs: List of chat log entries

        Returns:
            HuggingFace Dataset object

        Note:
            Ragas requires the following fields:
            - question: The user's question
            - contexts: List of retrieved context chunks
            - answer: The generated response
        """
        # Prepare data in the format required by Ragas
        data = {
            "question": [log["question"] for log in logs],
            "contexts": [log["contexts"] for log in logs],
            "answer": [log["answer"] for log in logs]
        }

        dataset = Dataset.from_dict(data)
        logger.info(f"Converted {len(logs)} entries to HuggingFace Dataset")
        return dataset

    def evaluate_batch(self, dataset: Dataset) -> Dict[str, Any]:
        """
        Run Ragas evaluation on the dataset using faithfulness and answer_relevancy metrics.

        Args:
            dataset: HuggingFace Dataset containing questions, contexts, and answers

        Returns:
            Evaluation results from Ragas

        Note:
            This method uses:
            - faithfulness: Measures factual consistency of the answer with the contexts
            - answer_relevancy: Measures how relevant the answer is to the question

        Raises:
            ValueError: If OPENAI_API_KEY is not set
        """
        # Ensure OpenAI API key is set for Ragas
        if not settings.openai_api_key:
            error_msg = "OPENAI_API_KEY is not set. Please configure it in your .env file."
            logger.error(error_msg)
            raise ValueError(error_msg)

        # Set environment variable for Ragas to use
        os.environ["OPENAI_API_KEY"] = settings.openai_api_key

        logger.info("Starting Ragas evaluation with faithfulness and answer_relevancy metrics")

        try:
            # Run evaluation with specified metrics
            results = evaluate(
                dataset,
                metrics=[faithfulness, answer_relevancy]
            )
            logger.info("Ragas evaluation completed successfully")
            return results
        except Exception as e:
            logger.error(f"Ragas evaluation failed: {e}", exc_info=True)
            raise

    def calculate_average_scores(self, results: Dict[str, Any], logs: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        """
        Calculate average score for each entry and update the log entries.

        Args:
            results: Evaluation results from Ragas
            logs: Original log entries

        Returns:
            Updated log entries with ragas_score field

        Note:
            The average score is calculated as: (faithfulness + answer_relevancy) / 2
        """
        # Extract metric scores from results
        faithfulness_scores = results.get("faithfulness", [])
        answer_relevancy_scores = results.get("answer_relevancy", [])

        # Update each log entry with the calculated score
        for i, log in enumerate(logs):
            faithfulness_score = faithfulness_scores[i] if i < len(faithfulness_scores) else 0.0
            relevancy_score = answer_relevancy_scores[i] if i < len(answer_relevancy_scores) else 0.0

            # Calculate average score
            average_score = (faithfulness_score + relevancy_score) / 2.0

            # Add score to log entry
            log["ragas_score"] = average_score
            log["faithfulness"] = faithfulness_score
            log["answer_relevancy"] = relevancy_score

        logger.info(f"Calculated average scores for {len(logs)} entries")
        return logs

    def save_scored_logs(self, all_logs: List[Dict[str, Any]], scored_logs: List[Dict[str, Any]]) -> None:
        """
        Save the scored results to a new output file.

        Args:
            all_logs: All original log entries
            scored_logs: Newly scored log entries

        Note:
            This creates a new file to prevent data corruption/locking on the original file.
        """
        # Create a mapping of scored entries by a unique key
        # Using combination of question, conversation_id, and timestamp for uniqueness
        scored_map = {}
        for log in scored_logs:
            key = f"{log.get('conversation_id', '')}_{log.get('timestamp', '')}"
            scored_map[key] = log

        # Update all_logs with scored entries
        updated_logs = []
        for log in all_logs:
            key = f"{log.get('conversation_id', '')}_{log.get('timestamp', '')}"
            if key in scored_map:
                updated_logs.append(scored_map[key])
            else:
                updated_logs.append(log)

        # Save to new file
        try:
            with open(self.output_file, 'w', encoding='utf-8') as f:
                json.dump(updated_logs, f, ensure_ascii=False, indent=2)
            logger.info(f"Saved scored results to {self.output_file}")
        except Exception as e:
            logger.error(f"Failed to save scored logs: {e}", exc_info=True)
            raise

    async def run_evaluation(self) -> Dict[str, Any]:
 
        try:
            # Step 1: Load logs
            all_logs = self.load_logs()

            # Step 2: Filter unevaluated entries
            unevaluated_logs = self.filter_unevaluated_logs(all_logs)

            if not unevaluated_logs:
                logger.info("No unevaluated entries found. Nothing to process.")
                return {
                    "processed": 0,
                    "file_saved": str(self.output_file),
                    "message": "No unevaluated entries found"
                }

            # Step 3: Convert to Dataset
            dataset = self.convert_to_dataset(unevaluated_logs)

            # Step 4: Run evaluation
            results = self.evaluate_batch(dataset)

            # Step 5: Calculate average scores
            scored_logs = self.calculate_average_scores(results, unevaluated_logs)

            # Step 6: Save results
            self.save_scored_logs(all_logs, scored_logs)

            # Step 7: Return summary
            summary = {
                "processed": len(scored_logs),
                "file_saved": str(self.output_file),
                "message": f"Successfully evaluated {len(scored_logs)} entries"
            }
            logger.info(f"Evaluation complete: {summary}")
            return summary

        except Exception as e:
            logger.error(f"Evaluation process failed: {e}", exc_info=True)
            raise


def get_evaluation_service(input_file: str = "chat_logs.json", output_file: str = "chat_logs_scored.json") -> EvaluationService:
    """
    Factory function to create an EvaluationService instance.

    Args:
        input_file: Path to the input JSON file
        output_file: Path to the output JSON file

    Returns:
        EvaluationService instance
    """
    return EvaluationService(input_file, output_file)
